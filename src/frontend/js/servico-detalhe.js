import { api } from "./api.js?v=4";
import { podeExecutarAcao } from "./auth.js?v=4";
import { CONFIG } from "./config.js?v=4";
import { inicializarLayout } from "./layout.js?v=4";
import {
  ajustarFonteCards, badgeStatus, escapar, esconderMensagem, formatarDataHora, formatarIntervalo,
  formatarMs, mostrarMensagem, obterParametroUrl, tempoRelativo,
} from "./utils.js?v=4";

const sessao = inicializarLayout({ telaAtiva: "servico-detalhe", abaAtiva: "dashboard" });
const id = obterParametroUrl("id");

const el = {
  mensagem: document.getElementById("mensagem"),
  nome: document.getElementById("nome"),
  url: document.getElementById("url"),
  statusAtual: document.getElementById("status-atual"),
  ultimaResposta: document.getElementById("ultima-resposta"),
  rotuloDisponibilidade: document.getElementById("rotulo-disponibilidade"),
  disponibilidade: document.getElementById("disponibilidade"),
  rotuloTempoMedio: document.getElementById("rotulo-tempo-medio"),
  tempoMedio: document.getElementById("tempo-medio"),
  graficoLatencia: document.getElementById("grafico-latencia"),
  barraUptime: document.getElementById("barra-uptime"),
  resumoGrafico: document.getElementById("resumo-grafico"),
  seletorPeriodo: document.getElementById("seletor-periodo"),
  detalhes: document.getElementById("detalhes"),
  historico: document.getElementById("tabela-historico"),
  btnVerificar: document.getElementById("btn-verificar"),
  btnAtivo: document.getElementById("btn-ativo"),
  btnEditar: document.getElementById("btn-editar"),
  btnExcluir: document.getElementById("btn-excluir"),
};

let servicoAtual = null;
let periodoAtual = "24h";

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
  el.rotuloDisponibilidade.textContent = `Disponibilidade (${periodoAtual})`;
  el.rotuloTempoMedio.textContent = `Resposta média (${periodoAtual})`;
  el.disponibilidade.textContent = m.disponibilidadePercentual == null ? "--" : `${m.disponibilidadePercentual}%`;
  el.tempoMedio.textContent = m.tempoMedioRespostaMs == null ? "--" : `${Math.round(m.tempoMedioRespostaMs)} ms`;

  const faixa = m.tempoMinimoRespostaMs != null && m.tempoMaximoRespostaMs != null
    ? ` · Mín: ${m.tempoMinimoRespostaMs} ms / Máx: ${m.tempoMaximoRespostaMs} ms (${m.totalVerificacoes} checagens em ${periodoAtual})`
    : ` (${m.totalVerificacoes} checagens em ${periodoAtual})`;
  el.resumoGrafico.textContent = `Últimas verificações${faixa}`;
}

/**
 * Renderiza o gráfico SVG de curva de latência e a barra visual de uptime (estilo UptimeRobot/Statuspage)
 * sem depender de bibliotecas externas bloqueáveis por CDN.
 */
function renderizarGrafico(verificacoes) {
  // Ordena cronologicamente (mais antigas à esquerda, mais recentes à direita)
  const cronologico = [...verificacoes].reverse();

  // Barra de blocos de status (uptime bar)
  if (cronologico.length === 0) {
    el.barraUptime.innerHTML = "";
    el.graficoLatencia.innerHTML = '<div class="vazio">Sem dados suficientes para exibir o gráfico.</div>';
    return;
  }

  el.barraUptime.innerHTML = cronologico.map((v) => {
    const classe = (v.status || "Desconhecido").toLowerCase();
    const titulo = `${formatarDataHora(v.verificadoEm)} — ${v.status} (${formatarMs(v.tempoRespostaMs)})`;
    return `<div class="bloco ${classe}" title="${escapar(titulo)}"></div>`;
  }).join("");

  const comTempo = cronologico.filter((v) => v.tempoRespostaMs != null);
  if (comTempo.length === 0) {
    el.graficoLatencia.innerHTML = '<div class="vazio">Nenhuma resposta com tempo registrado nas últimas verificações.</div>';
    return;
  }

  const largura = 760;
  const altura = 150;
  const maxMs = Math.max(...comTempo.map((v) => v.tempoRespostaMs), 50);

  const pontos = cronologico.map((v, idx) => {
    const x = cronologico.length === 1 ? largura / 2 : (idx / (cronologico.length - 1)) * largura;
    const ms = v.tempoRespostaMs ?? maxMs;
    const y = altura - Math.min(1, ms / maxMs) * (altura - 24) - 10;
    return { x: Math.round(x * 10) / 10, y: Math.round(y * 10) / 10, v };
  });

  const polyline = pontos.map((p) => `${p.x},${p.y}`).join(" ");
  const area = `0,${altura} ${polyline} ${largura},${altura}`;

  const circulos = pontos.map((p) => {
    const cor = p.v.status === "Offline" ? "#ef4444" : p.v.status === "Atencao" ? "#eab308" : "#3b82f6";
    const dica = `${formatarDataHora(p.v.verificadoEm)}: ${p.v.status} (${formatarMs(p.v.tempoRespostaMs)})`;
    return `<circle cx="${p.x}" cy="${p.y}" r="3.8" fill="${cor}"><title>${escapar(dica)}</title></circle>`;
  }).join("");

  el.graficoLatencia.innerHTML = `
    <svg class="grafico-svg" viewBox="0 0 ${largura} ${altura}" preserveAspectRatio="none">
      <defs>
        <linearGradient id="gradLatencia" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stop-color="#3b82f6" stop-opacity="0.35"/>
          <stop offset="100%" stop-color="#3b82f6" stop-opacity="0.0"/>
        </linearGradient>
      </defs>
      <line x1="0" y1="14" x2="${largura}" y2="14" stroke="#2a3744" stroke-dasharray="4 4"/>
      <text x="4" y="11" fill="#8b98a5" font-size="10">${maxMs} ms</text>
      <polygon points="${area}" fill="url(#gradLatencia)"/>
      <polyline fill="none" stroke="#3b82f6" stroke-width="2" points="${polyline}"/>
      ${circulos}
    </svg>`;
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
      api.metricas(id, periodoAtual),
      api.verificacoes(id, 40),
    ]);
    esconderMensagem(el.mensagem);
    renderizarServico(servico);
    renderizarMetricas(metricas);
    renderizarGrafico(verificacoes);
    renderizarHistorico(verificacoes);
    requestAnimationFrame(ajustarFonteCards);
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

el.seletorPeriodo.querySelectorAll("[data-periodo]").forEach((btn) => {
  btn.addEventListener("click", () => {
    periodoAtual = btn.dataset.periodo;
    el.seletorPeriodo.querySelectorAll("[data-periodo]").forEach((b) => b.classList.remove("ativo"));
    btn.classList.add("ativo");
    carregar();
  });
});

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

if (!podeExecutarAcao("editar-servico", sessao)) {
  el.btnAtivo.classList.add("oculto");
  el.btnEditar.classList.add("oculto");
  el.btnExcluir.classList.add("oculto");
}

if (!id) {
  mostrarMensagem(el.mensagem, "Serviço não informado. Volte ao dashboard e escolha um serviço.");
} else {
  carregar();
  setInterval(carregar, CONFIG.INTERVALO_ATUALIZACAO_MS);
}
