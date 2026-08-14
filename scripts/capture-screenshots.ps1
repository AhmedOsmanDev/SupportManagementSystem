param(
    [string]$WebUrl = "http://localhost:4200",
    [string]$ApiUrl = "http://localhost:5052",
    [string]$OutputDirectory = "docs/screenshots"
)

$ErrorActionPreference = "Stop"
$edgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
if (-not (Test-Path -LiteralPath $edgePath)) {
    throw "Microsoft Edge was not found at $edgePath."
}

$outputPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\$OutputDirectory"))
[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
$profilePath = Join-Path ([System.IO.Path]::GetTempPath()) "sms-edge-capture-$([Guid]::NewGuid().ToString('N'))"
[System.IO.Directory]::CreateDirectory($profilePath) | Out-Null
$debugPort = 9333
$edgeProcess = $null
$socket = $null
$nextCommandId = 0

function Send-CdpCommand {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [hashtable]$Parameters = @{}
    )

    $script:nextCommandId++
    $commandId = $script:nextCommandId
    $message = @{ id = $commandId; method = $Method; params = $Parameters } |
        ConvertTo-Json -Compress -Depth 20
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($message)
    $segment = [System.ArraySegment[byte]]::new($bytes)
    $socket.SendAsync(
        $segment,
        [System.Net.WebSockets.WebSocketMessageType]::Text,
        $true,
        [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    while ($true) {
        $buffer = New-Object byte[] (8 * 1024 * 1024)
        $receiveSegment = [System.ArraySegment[byte]]::new($buffer)
        $received = $socket.ReceiveAsync(
            $receiveSegment,
            [Threading.CancellationToken]::None).GetAwaiter().GetResult()
        $json = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)
        $response = $json | ConvertFrom-Json
        if ($response.id -eq $commandId) {
            if ($response.error) {
                throw "CDP $Method failed: $($response.error.message)"
            }
            return $response.result
        }
    }
}

function Navigate-And-Wait {
    param(
        [Parameter(Mandatory)] [string]$Url,
        [Parameter(Mandatory)] [string]$ExpectedText
    )

    Send-CdpCommand -Method "Page.navigate" -Parameters @{ url = $Url } | Out-Null
    $deadline = (Get-Date).AddSeconds(20)
    $textLiteral = $ExpectedText | ConvertTo-Json -Compress
    do {
        Start-Sleep -Milliseconds 300
        $evaluation = Send-CdpCommand -Method "Runtime.evaluate" -Parameters @{
            expression = "Boolean(document.body && document.body.innerText.includes($textLiteral))"
            returnByValue = $true
        }
        if ($evaluation.result.value -eq $true) {
            Send-CdpCommand -Method "Runtime.evaluate" -Parameters @{
                expression = "window.scrollTo(0, 0); document.querySelector('.mat-drawer-content')?.scrollTo(0, 0)"
            } | Out-Null
            Start-Sleep -Milliseconds 800
            return
        }
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for '$ExpectedText' at $Url."
}

function Set-Session {
    param([Parameter(Mandatory)] [object]$Session)

    $sessionJson = $Session | ConvertTo-Json -Compress -Depth 10
    $sessionLiteral = $sessionJson | ConvertTo-Json -Compress
    Send-CdpCommand -Method "Runtime.evaluate" -Parameters @{
        expression = "localStorage.setItem('sms.auth.session', $sessionLiteral)"
        returnByValue = $true
    } | Out-Null
}

function Capture-Screenshot {
    param([Parameter(Mandatory)] [string]$FileName)

    $capture = Send-CdpCommand -Method "Page.captureScreenshot" -Parameters @{
        format = "png"
        fromSurface = $true
        captureBeyondViewport = $false
    }
    [System.IO.File]::WriteAllBytes(
        (Join-Path $outputPath $FileName),
        [Convert]::FromBase64String($capture.data))
    Write-Output "Captured $FileName"
}

function Capture-ElementScreenshot {
    param(
        [Parameter(Mandatory)] [string]$FileName,
        [Parameter(Mandatory)] [string]$Selector
    )

    $selectorLiteral = $Selector | ConvertTo-Json -Compress
    $evaluation = Send-CdpCommand -Method "Runtime.evaluate" -Parameters @{
        expression = "JSON.stringify((() => { const r = document.querySelector($selectorLiteral).getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; })())"
        returnByValue = $true
    }
    $rectangle = $evaluation.result.value | ConvertFrom-Json
    $capture = Send-CdpCommand -Method "Page.captureScreenshot" -Parameters @{
        format = "png"
        fromSurface = $true
        captureBeyondViewport = $true
        clip = @{
            x = $rectangle.x
            y = $rectangle.y
            width = $rectangle.width
            height = $rectangle.height
            scale = 1
        }
    }
    [System.IO.File]::WriteAllBytes(
        (Join-Path $outputPath $FileName),
        [Convert]::FromBase64String($capture.data))
    Write-Output "Captured $FileName"
}

function Login {
    param([string]$Email, [string]$Password)

    $body = @{ email = $Email; password = $Password } | ConvertTo-Json
    return Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $body
}

try {
    $arguments = @(
        "--headless=new",
        "--disable-gpu",
        "--hide-scrollbars",
        "--no-first-run",
        "--remote-debugging-port=$debugPort",
        "--user-data-dir=$profilePath",
        "--window-size=1440,1000",
        "$WebUrl/login"
    )
    $edgeProcess = Start-Process -FilePath $edgePath -ArgumentList $arguments -WindowStyle Hidden -PassThru

    $deadline = (Get-Date).AddSeconds(20)
    do {
        try {
            $targets = Invoke-RestMethod -Uri "http://127.0.0.1:$debugPort/json" -TimeoutSec 2
            $target = $targets | Where-Object { $_.type -eq "page" } | Select-Object -First 1
        }
        catch {
            $target = $null
        }
        if (-not $target) { Start-Sleep -Milliseconds 250 }
    } while (-not $target -and (Get-Date) -lt $deadline)
    if (-not $target) { throw "The Edge debugging target did not start." }

    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    [void]$socket.ConnectAsync(
        [Uri]$target.webSocketDebuggerUrl,
        [Threading.CancellationToken]::None).GetAwaiter().GetResult()
    Send-CdpCommand -Method "Page.enable" | Out-Null
    Send-CdpCommand -Method "Runtime.enable" | Out-Null
    Send-CdpCommand -Method "Emulation.setDeviceMetricsOverride" -Parameters @{
        width = 1440
        height = 1000
        deviceScaleFactor = 1
        mobile = $false
    } | Out-Null

    Navigate-And-Wait -Url "$WebUrl/login" -ExpectedText "Welcome back"
    Send-CdpCommand -Method "Runtime.evaluate" -Parameters @{
        expression = "localStorage.removeItem('sms.auth.session')"
    } | Out-Null
    Capture-Screenshot "01-login.png"

    $customer = Login "customer@support.local" "Customer123!"
    Set-Session $customer
    Navigate-And-Wait -Url "$WebUrl/tickets" -ExpectedText "Your support requests"
    Capture-Screenshot "02-customer-ticket-list.png"
    Navigate-And-Wait -Url "$WebUrl/tickets/new" -ExpectedText "How can we help?"
    Capture-Screenshot "03-create-ticket.png"

    $agent = Login "agent@support.local" "Agent123!"
    Set-Session $agent
    Navigate-And-Wait -Url "$WebUrl/tickets/1" -ExpectedText "Checkout unavailable"
    Send-CdpCommand -Method "Runtime.evaluate" -Parameters @{
        expression = "Array.from(document.querySelectorAll('[role=tab]')).find(tab => tab.textContent.includes('Activity'))?.click()"
    } | Out-Null
    Start-Sleep -Milliseconds 800
    Capture-Screenshot "04-ticket-timeline-and-time.png"

    $admin = Login "admin@support.local" "Admin123!"
    Set-Session $admin
    Navigate-And-Wait -Url "$WebUrl/dashboard" -ExpectedText "Support dashboard"
    Capture-Screenshot "05-admin-dashboard.png"
    Capture-ElementScreenshot "06-agent-workload.png" ".dashboard-grid"

    Navigate-And-Wait -Url "$ApiUrl/swagger/index.html" -ExpectedText "Support Ticket Management API"
    Capture-Screenshot "07-swagger.png"
}
finally {
    if ($socket) {
        $socket.Dispose()
    }
    if ($edgeProcess -and -not $edgeProcess.HasExited) {
        Stop-Process -Id $edgeProcess.Id -Force
    }
    if (Test-Path -LiteralPath $profilePath) {
        Remove-Item -LiteralPath $profilePath -Recurse -Force -ErrorAction SilentlyContinue
    }
}
