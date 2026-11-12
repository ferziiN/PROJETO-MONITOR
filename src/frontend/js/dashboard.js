import { api } from "./api.js";
import { CONFIG } from "./config.js";
import {
  badgeStatus, escapar, esconderMensagem, formatarIntervalo,
  formatarMs, mostrarMensagem, tempoRelativo,
} from "./utils.js";

const el = {
  mensagem: document.getElementById("mensagem"),
  ultimaAtualizacao: document.getElementById("ultima-atualizacao"),
  tabela: document.getElementById("tabela-servicos"),
  totalAtivos: document.getElementById("total-ativos"),
  totalOnline: document.getElementById("total-online"),
  totalOffline: document.getElementById("total-offline"),
  totalAtencao: document.getElementById("total-atencao"),
  tempoMedio: document.getElementById("tempo-medio"),
  btnAtualizar: document.getElementById("btn-atualizar"),
};

function renderizarResumo(d) {
  el.totalAtivos.textContent = d.ativos;
  el.totalOnline.textContent = d.online;
  el.totalOffline.textContent = d.offline;
  el.totalAtencao.textContent = d.atencao;
  el.tempoMedio.textContent = d.tempoMedioRespostaMs == null ? "--" : `${Math.round(d.tempoMedioRespostaMs)} ms`;
}

function renderizarTabela(servicos) {
  if (servicos.length === 0) {
    el.tabela.innerHTML = `
      <tr><td colspan="6" class="vazio">
        Nenhum serviço cadastrado. <a href="servico-form.html">Cadastre o primeiro</a>.
      </td></tr>`;
    return;
  }

  el.tabela.innerHTML = servicos.map((s) => `
    <tr class="${s.ativo ? "" : "inativo"}">
      <td>
        <a href="servico.html?id=${s.id}"><strong>${escapar(s.nome)}</strong></a><br>
        <span class="mono suave">${escapar(s.url)}</span>
      </td>
      <td>${s.ativo ? badgeStatus(s.statusAtual) : '<span class="suave">Pausado</span>'}</td>
      <td class="mono">${formatarMs(s.ultimoTempoRespostaMs)}</td>
      <td class="suave">${tempoRelativo(s.ultimaVerificacaoEm)}</td>
      <td class="suave">${formatarIntervalo(s.intervaloSegundos)}</td>
      <td><a class="botao pequeno" href="servico.html?id=${s.id}">Detalhes</a></td>
    </tr>`).join("");
}

async function carregar() {
  try {
    const dados = await api.dashboard();
    esconderMensagem(el.mensagem);
    renderizarResumo(dados);
    renderizarTabela(dados.servicos);
    el.ultimaAtualizacao.textContent =
      `Atualizado às ${new Date().toLocaleTimeString("pt-BR")} · atualização automática a cada ${CONFIG.INTERVALO_ATUALIZACAO_MS / 1000}s`;
  } catch (erro) {
    mostrarMensagem(el.mensagem, erro.message);
  }
}

el.btnAtualizar.addEventListener("click", carregar);

carregar();
setInterval(carregar, CONFIG.INTERVALO_ATUALIZACAO_MS);

