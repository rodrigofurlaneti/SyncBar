# syntax=docker/dockerfile:1

# ==========================================================================
# Build multi-stage da SyncBar.API — contexto de build: backend/src
#   docker build -f backend/src/Dockerfile -t syncbar-api ./backend/src
# ==========================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia apenas os .csproj primeiro para aproveitar o cache de layer do
# "dotnet restore" (so reexecuta o restore se as dependencias mudarem).
COPY *.sln ./
COPY SyncBar.Domain/SyncBar.Domain.csproj SyncBar.Domain/
COPY SyncBar.Application/SyncBar.Application.csproj SyncBar.Application/
COPY SyncBar.Infrastructure/SyncBar.Infrastructure.csproj SyncBar.Infrastructure/
COPY SyncBar.API/SyncBar.API.csproj SyncBar.API/

# Restaura somente a API (traz as dependencias transitivas). Os projetos de
# teste ficam em backend/test (fora deste contexto) e nao entram na imagem.
RUN dotnet restore SyncBar.API/SyncBar.API.csproj

# Copia o restante do codigo-fonte e publica somente a API (Release).
COPY . .
RUN dotnet publish SyncBar.API/SyncBar.API.csproj -c Release -o /app/publish --no-restore

# ==========================================================================
# Stage final — imagem de runtime enxuta (ASP.NET, sem SDK), usuario nao-root
# ==========================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN useradd --uid 1000 --user-group --create-home appuser

COPY --from=build /app/publish .

# Diretorio de uploads (imagens do cardapio) gravavel pelo usuario da app.
RUN mkdir -p /app/wwwroot/uploads && chown -R appuser:appuser /app/wwwroot
USER appuser

# A API escuta na 8080 dentro do container.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Healthcheck no endpoint mapeado em Program.cs (app.MapHealthChecks("/health")).
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SyncBar.API.dll"]
