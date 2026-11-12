import { CONFIG } from "./config.js";

/**
 * Erro lançado quando a API responde com status de erro.
 * `detalhes` contém o ProblemDetails retornado (title, detail, errors...).
 */
export class ErroApi extends Error {
  constructor(status, detalhes) {
    super(detalhes?.detail || detalhes?.title || `Erro HTTP ${status}`);
    this.status = status;
    this.detalhes = detalhes;
    /** Erros de validação por campo: { Nome: ["..."], Endereco: ["..."] } */
    this.errosCampos = detalhes?.errors ?? {};
  }
}

async function requisitar(caminho, { metodo = "GET", corpo } = {}) {
  let resposta;
  try {
    resposta = await fetch(`${CONFIG.API_URL}${caminho}`, {
      method: metodo,
      headers: corpo ? { "Content-Type": "application/json" } : undefined,
      body: corpo ? JSON.stringify(corpo) : undefined,
    });
  } catch {
    throw new ErroApi(0, { title: "Não foi possível conectar à API. Ela está em execução?" });
  }

  if (resposta.status === 204) return null;

  const texto = await resposta.text();
  const dados = texto ? JSON.parse(texto) : null;

  if (!resposta.ok) throw new ErroApi(resposta.status, dados);
  return dados;
}

/** Cliente da API REST do Monitor. */
export const api = {
  dashboard: () => requisitar("/api/dashboard"),

  listarServicos: () => requisitar("/api/servicos"),
  obterServico: (id) => requisitar(`/api/servicos/${id}`),
  criarServico: (dados) => requisitar("/api/servicos", { metodo: "POST", corpo: dados }),
  atualizarServico: (id, dados) => requisitar(`/api/servicos/${id}`, { metodo: "PUT", corpo: dados }),
  alterarMonitoramento: (id, ativo) =>
    requisitar(`/api/servicos/${id}/ativo`, { metodo: "PATCH", corpo: { ativo } }),
  removerServico: (id) => requisitar(`/api/servicos/${id}`, { metodo: "DELETE" }),

  statusServico: (id) => requisitar(`/api/servicos/${id}/status`),
  verificacoes: (id, limite = 50) => requisitar(`/api/servicos/${id}/verificacoes?limite=${limite}`),
  metricas: (id, periodo = "24h") => requisitar(`/api/servicos/${id}/metricas?periodo=${periodo}`),
  verificarAgora: (id) => requisitar(`/api/servicos/${id}/verificar`, { metodo: "POST" }),
};

