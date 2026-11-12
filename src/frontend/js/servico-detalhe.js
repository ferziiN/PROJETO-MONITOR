import { api } from "./api.js";
import { CONFIG } from "./config.js";
import {
  badgeStatus, escapar, esconderMensagem, formatarDataHora, formatarIntervalo,
  formatarMs, mostrarMensagem, obterParametroUrl, tempoRelativo,
} from "./utils.js";

const id = obterParametroUrl("id");

const el = {
  mensagem: document.getElementById("mensagem"),
  nome: document.getElementById("nome"),
  url: document.getElementById("url"),
  statusAtual: document.getElementById("status-atual"),
  ultimaResposta: document.getElementById("ultima-resposta"),
  disponibilidade: document.getElementById("disponibilidade"),
  tempoMedio: document.getElementById("tempo-medio"),
  detalhes: document.getElementById("detalhes"),
  historico: document.getElementById("tabela-historico"),
  btnVerificar: document.getElementById("btn-verificar"),
  btnAtivo: document.getElementById("btn-ativo"),
  btnEditar: document.getElementById("btn-editar"),
  btnExcluir: document.getElementById("btn-excluir"),
};

let servicoAtual = null;

function renderizarServico(s) {
  servicoAtual = s;
  document.title = `${s.nome} · Monitor`;
  el.nome.textContent = s.nome;
  el.url.textContent = s.url;
  el.statusAtual.innerHTML = s.ativo ? badgeStatus(s.statusAtual) : '<span class="suave">Pausado</span>';
  el.ultimaResposta.textContent = formatarMs(s.ultimoTempoRespostaMs);
  el.btnAtivo.textContent = s.ativo ? "⏸ Pausar" : "▶ Retomar";
  el.btnEditar.href = `servico-form.html?id=${s.id}`;

  const itens = [
    ["Protocolo", s.tipo.toUpperCase()],
    ["Endereço", s.endereco],
    ["Porta", s.porta ?? "Padrão"],
    ["Endpoint", s.endpoint],
    ["Intervalo", formatarIntervalo(s.intervaloSegundos)],
    ["Monitoramento", s.ativo ? "Ativo" : "Pausado"],
    ["Última verificação", tempoRelativo(s.ultimaVerificacaoEm)],
    ["Próxima verificação", s.proximaVerificacaoEm ? formatarDataHora(s.proximaVerificacaoEm) : "--"],
    ["Cadastrado em", formatarDataHora(s.criadoEm)],
  ];
  el.detalhes.innerHTML = itens
    .map(([rotulo, valor]) => `<div><dt>${rotulo}</dt><dd>${escapar(valor)}</dd></div>`)
    .join("");
}

function renderizarMetricas(m) {
  el.disponibilidade.textContent = m.disponibilidadePercentual == null ? "--" : `${m.disponibilidadePercentual}%`;
  el.tempoMedio.textContent = m.tempoMedioRespostaMs == null ? "--" : `${Math.round(m.tempoMedioRespostaMs)} ms`;
}

function renderizarHistorico(verificacoes) {
  if (verificacoes.length === 0) {
    el.historico.innerHTML = '<tr><td colspan="5" class="vazio">Nenhuma verificação registrada ainda.</td></tr>';
    return;
  }
  el.historico.innerHTML = verificacoes.map((v) => `
    <tr>
      <td>${formatarDataHora(v.verificadoEm)}</td>
      <td>${badgeStatus(v.status)}</td>
      <td class="mono">${formatarMs(v.tempoRespostaMs)}</td>
      <td class="mono">${v.codigoHttp ?? "--"}</td>
      <td class="suave">${escapar(v.mensagemErro ?? "")}</td>
    </tr>`).join("");
}

async function carregar() {
  try {
    const [servico, metricas, verificacoes] = await Promise.all([
      api.obterServico(id),
      api.metricas(id, "24h"),
      api.verificacoes(id, 30),
    ]);
    esconderMensagem(el.mensagem);
    renderizarServico(servico);
    renderizarMetricas(metricas);
    renderizarHistorico(verificacoes);
  } catch (erro) {
    mostrarMensagem(el.mensagem, erro.message);
  }
}

async function executarAcao(botao, acao) {
  botao.disabled = true;
  try {
    const msgSucesso = await acao();
    await carregar();
    if (typeof msgSucesso === "string") mostrarMensagem(el.mensagem, msgSucesso, "sucesso");
  } catch (erro) {
    mostrarMensagem(el.mensagem, erro.message);
  } finally {
    botao.disabled = false;
  }
}

el.btnVerificar.addEventListener("click", () =>
  executarAcao(el.btnVerificar, async () => {
    const v = await api.verificarAgora(id);
    return `Verificação concluída: ${v.status} (${formatarMs(v.tempoRespostaMs)})`;
  }));

el.btnAtivo.addEventListener("click", () =>
  executarAcao(el.btnAtivo, () => api.alterarMonitoramento(id, !servicoAtual?.ativo)));

el.btnExcluir.addEventListener("click", async () => {
  if (!confirm(`Excluir "${servicoAtual?.nome}" e todo o seu histórico?`)) return;
  try {
    await api.removerServico(id);
    window.location.href = "dashboard.html";
  } catch (erro) {
    mostrarMensagem(el.mensagem, erro.message);
  }
});

if (!id) {
  mostrarMensagem(el.mensagem, "Serviço não informado. Volte ao dashboard e escolha um serviço.");
} else {
  carregar();
  setInterval(carregar, CONFIG.INTERVALO_ATUALIZACAO_MS);
}
