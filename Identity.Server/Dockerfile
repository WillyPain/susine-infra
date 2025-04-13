FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
COPY .dev-certs/ /usr/local/share/ca-certificates/
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS publish
WORKDIR /src
COPY . .
WORKDIR /src/Identity.Server
RUN dotnet restore
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -c Release -o /app

FROM base as final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Identity.Server.dll"]
