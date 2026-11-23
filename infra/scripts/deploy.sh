#!/usr/bin/env bash
# =============================================================================
# Script de Deploy Automatizado — Projeto Monitor (Sprint 4)
# Uso (a partir da raiz do repositório no servidor Linux):
#   chmod +x infra/scripts/deploy.sh
#   ./infra/scripts/deploy.sh
# =============================================================================

set -euo pipefail

COMPOSE_FILE="infra/docker/docker-compose.yml"

echo "===================================================="
echo " Iniciando deploy do Projeto Monitor"
echo "===================================================="

if ! command -v docker >/dev/null 2>&1; then
  echo "[ERRO] Docker não encontrado. Instale o Docker antes de continuar."
  exit 1
fi

echo "[1/4] Atualizando imagens base..."
docker compose -f "$COMPOSE_FILE" pull postgres nginx

echo "[2/4] Compilando e subindo os containers (PostgreSQL, API e Nginx)..."
docker compose -f "$COMPOSE_FILE" up -d --build --remove-orphans

echo "[3/4] Aguardando verificação de saúde (/health)..."
TENTATIVAS=20
PORTA="${MONITOR_HTTP_PORT:-80}"

for i in $(seq 1 "$TENTATIVAS"); do
  if curl -fsS "http://localhost:${PORTA}/health" >/dev/null 2>&1; then
    echo " -> Aplicação saudável (Healthy)!"
    break
  fi
  echo " -> Aguardando inicialização... (${i}/${TENTATIVAS})"
  sleep 3
done

echo "[4/4] Status dos containers:"
docker compose -f "$COMPOSE_FILE" ps

echo "===================================================="
echo " Deploy concluído! Acesse: http://localhost:${PORTA}"
echo "===================================================="
