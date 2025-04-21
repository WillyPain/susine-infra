FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
COPY .dev-certs/ /usr/local/share/ca-certificates/
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS publish
WORKDIR /src
COPY ./MatchMaking.Api ./MatchMaking.Api
COPY ./MatchMaking.Api.Tests ./MatchMaking.Api.Tests
COPY ./MatchMaking.Contract ./MatchMaking.Contract
WORKDIR /src/MatchMaking.Api
RUN dotnet restore
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -o /app /p:UseAppHost=false
WORKDIR /src/MatchMaking.Api.Tests
RUN dotnet test

FROM publish as dev
COPY .dev-certs/ /usr/local/share/ca-certificates/
RUN apk add --no-cache ca-certificates && update-ca-certificates
WORKDIR /app
ENTRYPOINT ["dotnet", "MatchMaking.Api.dll"]

FROM base as final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MatchMaking.Api.dll"]
