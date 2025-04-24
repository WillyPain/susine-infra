ARG APP_NAME=MatchMaking.Api

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
COPY .dev-certs/ /usr/local/share/ca-certificates/
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app

## Build stage one - Restore env
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine as restore-env
ARG APP_NAME
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global --no-cache dotnet-subset
WORKDIR /restore
COPY . .
WORKDIR ./${APP_NAME}
RUN dotnet subset restore ${APP_NAME}.csproj --root-directory /restore --output /restore/restore_subset/

## Build stage two - restore
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine as publish
ARG APP_NAME
WORKDIR /src
COPY --from=restore-env /restore/restore_subset/ .
RUN dotnet restore ./${APP_NAME}/${APP_NAME}.csproj
## Build stage three - build src
COPY . .
RUN dotnet publish ./${APP_NAME}/${APP_NAME}.csproj -o /app /p:UseAppHost=false
RUN dotnet test ./${APP_NAME}.Tests/${APP_NAME}.Tests.csproj


FROM publish as dev
WORKDIR /app
ENTRYPOINT ["dotnet", "MatchMaking.Api.dll"]

FROM base as final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MatchMaking.Api.dll"]
