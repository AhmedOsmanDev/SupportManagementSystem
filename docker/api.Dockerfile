FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY SupportManagementSystem.slnx ./
COPY src/SMS.API/SMS.API.csproj src/SMS.API/
COPY src/SMS.Application/SMS.Application.csproj src/SMS.Application/
COPY src/SMS.Domain/SMS.Domain.csproj src/SMS.Domain/
COPY src/SMS.Infrastructure/SMS.Infrastructure.csproj src/SMS.Infrastructure/
RUN dotnet restore src/SMS.API/SMS.API.csproj

COPY src/ src/
RUN dotnet publish src/SMS.API/SMS.API.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SMS.API.dll"]
