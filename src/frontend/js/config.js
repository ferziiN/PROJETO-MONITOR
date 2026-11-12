/**
 * Configuração do front-end.
 *
 * - Servido pela própria API (dev, http://localhost:5094) ou pelo Nginx (produção):
 *   usa caminho relativo ("") — mesma origem, sem CORS.
 * - Aberto por outro servidor local (ex.: Live Server na porta 5500) ou via file://:
 *   aponta para a API em http://localhost:5094.
 * - Para forçar outra URL: localStorage.setItem("monitor.apiUrl", "http://host:porta")
 */
const PORTA_API_DEV = "5094";

function resolverUrlApi() {
  const salva = localStorage.getItem("monitor.apiUrl");
  if (salva) return salva.replace(/\/$/, "");

  const { protocol, hostname, port } = window.location;
  const ehLocal = ["localhost", "127.0.0.1"].includes(hostname);

  if (protocol === "file:" || (ehLocal && port !== PORTA_API_DEV)) {
    return `http://localhost:${PORTA_API_DEV}`;
  }
  return "";
}

export const CONFIG = {
  API_URL: resolverUrlApi(),
  /** Intervalo de atualização automática das telas (ms). */
  INTERVALO_ATUALIZACAO_MS: 10000,
};

