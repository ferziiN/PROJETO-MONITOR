# =============================================================================
# Dockerfile multi-stage — Monitor.API (.NET 10)
# Contexto de build esperado: raiz do repositório
#   docker build -f infra/docker/Dockerfile -t monitor-api:latest .
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copia o csproj primeiro para aproveitar o cache de restore do Docker
COPY src/backend/Monitor.API/Monitor.API.csproj src/backend/Monitor.API/
RUN dotnet restore src/backend/Monitor.API/Monitor.API.csproj

# Copia o restante do código do back-end e publica em modo Release
COPY src/backend/Monitor.API/ src/backend/Monitor.API/
RUN dotnet publish src/backend/Monitor.API/Monitor.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Imagem final enxuta (apenas runtime ASP.NET Core)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Instala curl para healthcheck do container
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Monitor.API.dll"]
