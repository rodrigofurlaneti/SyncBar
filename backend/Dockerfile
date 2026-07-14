# syntax=docker/dockerfile:1
# Build multi-stage da SyncBar.API — roda a partir da pasta backend/ como contexto:
#   docker build -f backend/Dockerfile -t syncbar-api ./backend

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln ./
COPY SyncBar.API/*.csproj SyncBar.API/
COPY SyncBar.Application/*.csproj SyncBar.Application/
COPY SyncBar.Domain/*.csproj SyncBar.Domain/
COPY SyncBar.Infrastructure/*.csproj SyncBar.Infrastructure/
COPY SyncBar.ArchTests/*.csproj SyncBar.ArchTests/
COPY SyncBar.Tests/*.csproj SyncBar.Tests/
COPY SyncBar.Specs/*.csproj SyncBar.Specs/
RUN dotnet restore SyncBar.API/SyncBar.API.csproj

COPY . .
RUN dotnet publish SyncBar.API/SyncBar.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Usuário não-root
RUN useradd --uid 1000 --user-group --create-home appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SyncBar.API.dll"]
