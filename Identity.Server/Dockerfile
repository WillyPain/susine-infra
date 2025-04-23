FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
COPY .dev-certs/ /usr/local/share/ca-certificates/
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app

## Build stage one - Restore env
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine as restore-env
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global --no-cache dotnet-subset
WORKDIR /restore
COPY ./Identity.Server ./Identity.Server
WORKDIR ./Identity.Server
RUN dotnet subset restore Identity.Server.csproj --root-directory /restore --output /restore/restore_subset/

## Build stage two - restore
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine as publish
WORKDIR /src
COPY --from=restore-env /restore/restore_subset/ .
RUN dotnet restore ./Identity.Server/Identity.Server.csproj

## Build stage three - build src
COPY ./Identity.Server ./Identity.Server
RUN dotnet publish ./Identity.Server/Identity.Server.csproj -o /app /p:UseAppHost=false

FROM publish as dev
WORKDIR /app
ENTRYPOINT ["dotnet", "Identity.Server.dll"]

FROM base as final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Identity.Server.dll"]
