FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS publish
WORKDIR /src
COPY ./GameServerOrchestrator ./GameServerOrchestrator
WORKDIR /src/GameServerOrchestrator
RUN dotnet restore
RUN dotnet publish -o /app /p:UseAppHost=false

FROM publish as dev
COPY .dev-certs/ /usr/local/share/ca-certificates/
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app
ENTRYPOINT ["dotnet", "GameServerOrchestrator.dll"]

FROM base as final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GameServerOrchestrator.dll"]
