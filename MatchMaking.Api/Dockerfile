FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS publish
WORKDIR /src
COPY . .
WORKDIR /src/MatchMaking.Api
RUN dotnet restore
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -c Release -o /app
WORKDIR /src/MatchMaking.Api.Tests
RUN dotnet test

FROM susine.dev.base.image as final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MatchMaking.Api.dll"]
