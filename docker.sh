#!/bin/sh

set -eu

# Git Bash rewrites Linux container paths before invoking docker.exe unless this is disabled.
MSYS_NO_PATHCONV=1
MSYS2_ARG_CONV_EXCL='*'
export MSYS_NO_PATHCONV MSYS2_ARG_CONV_EXCL

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ENV_FILE=${ENV_FILE:-"$SCRIPT_DIR/.env"}

MANAGED_LABEL="com.support-management-system.managed"
API_IMAGE=${API_IMAGE:-"support-management-api:local"}
WEB_IMAGE=${WEB_IMAGE:-"support-management-web:local"}
SQL_IMAGE=${SQL_IMAGE:-"mcr.microsoft.com/mssql/server:2022-latest"}
NETWORK_NAME=${NETWORK_NAME:-"sms-network"}
VOLUME_NAME=${VOLUME_NAME:-"sms-sql-data"}
SQL_CONTAINER="sms-sql"
API_CONTAINER="sms-api"
WEB_CONTAINER="sms-web"

read_env_value() {
    key=$1
    if [ ! -f "$ENV_FILE" ]; then
        return 0
    fi

    awk -v wanted="$key" '
        BEGIN { prefix = wanted "=" }
        index($0, prefix) == 1 {
            print substr($0, length(prefix) + 1)
            exit
        }
    ' "$ENV_FILE" | tr -d '\r'
}

die() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

require_docker() {
    command -v docker >/dev/null 2>&1 || die "Docker is not installed or is not on PATH."
    docker info >/dev/null 2>&1 || die "Docker is not running. Start Docker Desktop or the Docker daemon."
}

validate_port() {
    name=$1
    value=$2
    case "$value" in
        ''|*[!0-9]*) die "$name must be a number between 1 and 65535." ;;
    esac
    [ "$value" -ge 1 ] && [ "$value" -le 65535 ] ||
        die "$name must be a number between 1 and 65535."
}

load_runtime_settings() {
    MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD:-$(read_env_value MSSQL_SA_PASSWORD)}
    JWT_SECRET=${JWT_SECRET:-$(read_env_value JWT_SECRET)}
    SQL_PORT=${SQL_PORT:-$(read_env_value SQL_PORT)}
    API_PORT=${API_PORT:-$(read_env_value API_PORT)}
    WEB_PORT=${WEB_PORT:-$(read_env_value WEB_PORT)}
    SQL_PORT=${SQL_PORT:-1433}
    API_PORT=${API_PORT:-5052}
    WEB_PORT=${WEB_PORT:-4200}

    [ -n "$MSSQL_SA_PASSWORD" ] || die "MSSQL_SA_PASSWORD is required in .env or the environment."
    [ "${#MSSQL_SA_PASSWORD}" -ge 8 ] || die "MSSQL_SA_PASSWORD must contain at least 8 characters."
    case "$MSSQL_SA_PASSWORD" in *[A-Z]*) ;; *) die "MSSQL_SA_PASSWORD must contain an uppercase letter." ;; esac
    case "$MSSQL_SA_PASSWORD" in *[a-z]*) ;; *) die "MSSQL_SA_PASSWORD must contain a lowercase letter." ;; esac
    case "$MSSQL_SA_PASSWORD" in *[0-9]*) ;; *) die "MSSQL_SA_PASSWORD must contain a number." ;; esac
    case "$MSSQL_SA_PASSWORD" in *[!A-Za-z0-9]*) ;; *) die "MSSQL_SA_PASSWORD must contain a symbol." ;; esac
    [ -n "$JWT_SECRET" ] || die "JWT_SECRET is required in .env or the environment."
    [ "${#JWT_SECRET}" -ge 32 ] || die "JWT_SECRET must contain at least 32 characters."
    [ "$MSSQL_SA_PASSWORD" != "Change_this_local_password_123!" ] ||
        die "Replace the example SQL Server password before starting the stack."
    [ "$JWT_SECRET" != "change-this-to-a-random-secret-at-least-32-characters-long" ] ||
        die "Replace the example JWT secret before starting the stack."

    validate_port SQL_PORT "$SQL_PORT"
    validate_port API_PORT "$API_PORT"
    validate_port WEB_PORT "$WEB_PORT"
}

container_exists() {
    docker container inspect "$1" >/dev/null 2>&1
}

assert_managed_container() {
    name=$1
    if container_exists "$name"; then
        owner=$(docker container inspect --format "{{ index .Config.Labels \"$MANAGED_LABEL\" }}" "$name")
        [ "$owner" = "true" ] || die "Container '$name' exists but is not managed by docker.sh. Rename or remove it first."
    fi
}

remove_managed_container() {
    name=$1
    if container_exists "$name"; then
        assert_managed_container "$name"
        docker rm --force "$name" >/dev/null
    fi
}

ensure_network() {
    if docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
        owner=$(docker network inspect --format "{{ index .Labels \"$MANAGED_LABEL\" }}" "$NETWORK_NAME")
        [ "$owner" = "true" ] || die "Network '$NETWORK_NAME' exists but is not managed by docker.sh."
        return
    fi

    docker network create --label "$MANAGED_LABEL=true" "$NETWORK_NAME" >/dev/null
}

ensure_volume() {
    if docker volume inspect "$VOLUME_NAME" >/dev/null 2>&1; then
        owner=$(docker volume inspect --format "{{ index .Labels \"$MANAGED_LABEL\" }}" "$VOLUME_NAME")
        [ "$owner" = "true" ] || die "Volume '$VOLUME_NAME' exists but is not managed by docker.sh."
        return
    fi

    docker volume create --label "$MANAGED_LABEL=true" "$VOLUME_NAME" >/dev/null
}

build_images() {
    require_docker
    printf 'Building API image %s...\n' "$API_IMAGE"
    docker build \
        --file "$SCRIPT_DIR/docker/api.Dockerfile" \
        --target final \
        --label "$MANAGED_LABEL=true" \
        --tag "$API_IMAGE" \
        "$SCRIPT_DIR"

    printf 'Building web image %s...\n' "$WEB_IMAGE"
    docker build \
        --file "$SCRIPT_DIR/docker/web.Dockerfile" \
        --target final \
        --label "$MANAGED_LABEL=true" \
        --tag "$WEB_IMAGE" \
        "$SCRIPT_DIR"
}

wait_for_health() {
    name=$1
    attempts=$2
    count=0

    while [ "$count" -lt "$attempts" ]; do
        status=$(docker container inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$name" 2>/dev/null || true)
        case "$status" in
            healthy) return 0 ;;
            unhealthy|exited|dead) return 1 ;;
        esac
        count=$((count + 1))
        sleep 2
    done

    return 1
}

wait_for_api() {
    docker run --rm \
        --network "$NETWORK_NAME" \
        --entrypoint /bin/sh \
        "$WEB_IMAGE" \
        -c 'attempt=0; until wget -q -O /dev/null http://api:8080/health; do attempt=$((attempt + 1)); [ "$attempt" -lt 90 ] || exit 1; sleep 2; done'
}

STARTED_SQL=false
STARTED_API=false
STARTED_WEB=false
STARTUP_COMPLETE=true

cleanup_failed_startup() {
    status=$?
    trap - EXIT HUP INT TERM
    if [ "$STARTUP_COMPLETE" = "false" ]; then
        printf '\nStartup failed. Recent container logs:\n' >&2
        for name in "$WEB_CONTAINER" "$API_CONTAINER" "$SQL_CONTAINER"; do
            if container_exists "$name"; then
                printf '\n--- %s ---\n' "$name" >&2
                docker logs --tail 40 "$name" >&2 2>/dev/null || true
            fi
        done
        remove_managed_container "$WEB_CONTAINER"
        remove_managed_container "$API_CONTAINER"
        remove_managed_container "$SQL_CONTAINER"
        printf '\nFailed containers were removed; the SQL data volume was preserved.\n' >&2
    fi
    exit "$status"
}

start_stack() {
    require_docker
    load_runtime_settings

    assert_managed_container "$WEB_CONTAINER"
    assert_managed_container "$API_CONTAINER"
    assert_managed_container "$SQL_CONTAINER"
    build_images

    remove_managed_container "$WEB_CONTAINER"
    remove_managed_container "$API_CONTAINER"
    remove_managed_container "$SQL_CONTAINER"
    ensure_network
    ensure_volume

    STARTED_SQL=false
    STARTED_API=false
    STARTED_WEB=false
    STARTUP_COMPLETE=false
    trap cleanup_failed_startup EXIT
    trap 'exit 130' HUP INT TERM

    printf 'Starting SQL Server...\n'
    docker run --detach \
        --name "$SQL_CONTAINER" \
        --network "$NETWORK_NAME" \
        --network-alias sqlserver \
        --label "$MANAGED_LABEL=true" \
        --label "com.support-management-system.component=sql" \
        --restart unless-stopped \
        --env ACCEPT_EULA=Y \
        --env MSSQL_PID=Developer \
        --env "MSSQL_SA_PASSWORD=$MSSQL_SA_PASSWORD" \
        --publish "127.0.0.1:$SQL_PORT:1433" \
        --volume "$VOLUME_NAME:/var/opt/mssql" \
        --health-cmd '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" || exit 1' \
        --health-interval 5s \
        --health-timeout 5s \
        --health-start-period 30s \
        --health-retries 30 \
        "$SQL_IMAGE" >/dev/null
    STARTED_SQL=true

    wait_for_health "$SQL_CONTAINER" 105 || die "SQL Server did not become healthy."

    escaped_password=$(printf '%s' "$MSSQL_SA_PASSWORD" | sed 's/"/""/g')
    connection_string="Server=sqlserver,1433;Database=SMS;User Id=sa;Password=\"$escaped_password\";Encrypt=True;TrustServerCertificate=True"

    printf 'Starting API...\n'
    docker run --detach \
        --name "$API_CONTAINER" \
        --network "$NETWORK_NAME" \
        --network-alias api \
        --label "$MANAGED_LABEL=true" \
        --label "com.support-management-system.component=api" \
        --restart unless-stopped \
        --env ASPNETCORE_ENVIRONMENT=Development \
        --env ASPNETCORE_HTTP_PORTS=8080 \
        --env "ConnectionStrings__DefaultConnection=$connection_string" \
        --env "Jwt__Secret=$JWT_SECRET" \
        --env Jwt__Issuer=SupportManagementSystem \
        --env Jwt__Audience=SupportManagementSystem.Client \
        --env Database__MigrateOnStartup=true \
        --env Database__SeedDemoData=true \
        --env "Cors__AllowedOrigins__0=http://localhost:$WEB_PORT" \
        --publish "127.0.0.1:$API_PORT:8080" \
        "$API_IMAGE" >/dev/null
    STARTED_API=true

    wait_for_api || die "The API did not become ready."

    printf 'Starting web client...\n'
    docker run --detach \
        --name "$WEB_CONTAINER" \
        --network "$NETWORK_NAME" \
        --network-alias web \
        --label "$MANAGED_LABEL=true" \
        --label "com.support-management-system.component=web" \
        --restart unless-stopped \
        --publish "127.0.0.1:$WEB_PORT:80" \
        "$WEB_IMAGE" >/dev/null
    STARTED_WEB=true

    wait_for_health "$WEB_CONTAINER" 30 || die "The web client did not become healthy."

    STARTUP_COMPLETE=true
    trap - EXIT HUP INT TERM
    printf '\nSupport Management System is ready:\n'
    printf '  Web:     http://localhost:%s\n' "$WEB_PORT"
    printf '  Swagger: http://localhost:%s/swagger\n' "$API_PORT"
    printf '  SQL:     localhost,%s\n' "$SQL_PORT"
}

stop_stack() {
    require_docker
    remove_managed_container "$WEB_CONTAINER"
    remove_managed_container "$API_CONTAINER"
    remove_managed_container "$SQL_CONTAINER"

    if docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
        owner=$(docker network inspect --format "{{ index .Labels \"$MANAGED_LABEL\" }}" "$NETWORK_NAME")
        [ "$owner" = "true" ] || die "Network '$NETWORK_NAME' exists but is not managed by docker.sh."
        docker network rm "$NETWORK_NAME" >/dev/null
    fi

    printf 'Containers stopped. SQL data remains in volume %s.\n' "$VOLUME_NAME"
}

reset_stack() {
    stop_stack
    if docker volume inspect "$VOLUME_NAME" >/dev/null 2>&1; then
        owner=$(docker volume inspect --format "{{ index .Labels \"$MANAGED_LABEL\" }}" "$VOLUME_NAME")
        [ "$owner" = "true" ] || die "Volume '$VOLUME_NAME' exists but is not managed by docker.sh."
        docker volume rm "$VOLUME_NAME" >/dev/null
        printf 'Deleted SQL volume %s. This data cannot be recovered.\n' "$VOLUME_NAME"
    fi
    start_stack
}

show_status() {
    require_docker
    docker ps --all \
        --filter "label=$MANAGED_LABEL=true" \
        --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}'
}

show_logs() {
    require_docker
    service=${1:-api}
    case "$service" in
        sql) name=$SQL_CONTAINER ;;
        api) name=$API_CONTAINER ;;
        web) name=$WEB_CONTAINER ;;
        *) die "Unknown service '$service'. Use sql, api, or web." ;;
    esac
    container_exists "$name" || die "Container '$name' does not exist."
    assert_managed_container "$name"
    docker logs --follow --tail 100 "$name"
}

show_help() {
    cat <<'EOF'
Usage: sh ./docker.sh [command]

Commands:
  up              Build images and start SQL Server, API, and web (default)
  build           Build the API and web images only; no .env is required
  down            Remove containers and network; preserve images and SQL data
  reset           Delete SQL data, rebuild, and start a fresh seeded stack
  status          Show managed containers and published ports
  logs [service]  Follow logs for api (default), web, or sql
  help            Show this help
EOF
}

command=${1:-up}
case "$command" in
    up) start_stack ;;
    build) build_images ;;
    down) stop_stack ;;
    reset) reset_stack ;;
    status) show_status ;;
    logs) show_logs "${2:-api}" ;;
    help|-h|--help) show_help ;;
    *) show_help; die "Unknown command '$command'." ;;
esac
