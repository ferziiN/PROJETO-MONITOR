import { api } from "./api.js?v=4";
import { ehAdmin, podeAcessarTela, podeExecutarAcao } from "./auth.js?v=4";
import { CONFIG } from "./config.js?v=4";
import { atualizarContadorAlertasSidebar, inicializarLayout } from "./layout.js?v=4";
import {
  ajustarFonteCards, badgeStatus, escapar, esconderMensagem, formatarIntervalo,
  formatarMs, mostrarMensagem, obterParametroUrl, tempoRelativo,
} from "./utils.js?v=4";

const sessao = inicializarLayout({ telaAtiva: "dashboard" });
const admin = ehAdmin(sessao);

const el = {
  saudacaoUsuario: document.getElementById("saudacao-usuario"),
  badgePerfilTopo: document.getElementById("badge-perfil-topo"),
  btnNovoServicoTopo: document.getElementById("btn-novo-servico-topo"),
  atalhoCadastrar: document.getElementById("atalho-cadastrar"),
  atalhoVerificarTodos: document.getElementById("atalho-verificar-todos"),
  mensagem: document.getElementById("mensagem"),
  ultimaAtualizacao: document.getElementById("ultima-atualizacao"),
  tabela: document.getElementById("tabela-servicos"),
  totalAtivos: document.getElementById("total-ativos"),
  totalOnline: document.getElementById("total-online"),
  totalOffline: document.getElementById("total-offline"),
  totalAtencao: document.getElementById("total-atencao"),
  tempoMedio: document.getElementById("tempo-medio"),
  totalAlertas: document.getElementById("total-alertas"),
  secaoAlertas: document.getElementById("secao-alertas"),
  listaAlertas: document.getElementById("lista-alertas"),
  btnReconhecerTodos: document.getElementById("btn-reconhecer-todos"),
  filtroBusca: document.getElementById("filtro-busca"),
  filtroStatus: document.getElementById("filtro-status"),
  btnAtualizar: document.getElementById("btn-atualizar"),
};

let listaServicosCache = [];

function aplicarContextoUsuario() {
  if (!sessao) return;

  el.saudacaoUsuario.textContent = `Olá, ${sessao.nome}`;
  el.badgePerfilTopo.textContent = admin ? "ADMIN · ACESSO TOTAL" : "USUÁRIO · MONITORAMENTO";
  el.badgePerfilTopo.className = `badge-perfil ${admin ? "admin" : "usuario"}`;

  if (!podeAcessarTela("servico-form", sessao)) {
    el.btnNovoServicoTopo.classList.add("oculto");
    el.atalhoCadastrar.classList.add("bloqueado");
    el.atalhoCadastrar.href = "#";
    el.atalhoCadastrar.addEventListener("click", (e) => {
      e.preventDefault();
      mostrarMensagem(
        el.mensagem,
        "O cadastro de novos serviços é exclusivo do perfil ADMIN. Os acessos do perfil USUÁRIO são controlados pela equipe.",
      );
    });
  }

  if (!podeExecutarAcao("reconhecer-alertas", sessao)) {
    el.btnReconhecerTodos.classList.add("oculto");
  }

  if (obterParametroUrl("acessoNegado") === "1") {
    mostrarMensagem(
      el.mensagem,
      "Acesso restrito ao perfil ADMIN. Você foi redirecionado para a Home.",
    );
  }
}

function renderizarResumo(d) {
  el.totalAtivos.textContent = d.ativos;
  el.totalOnline.textContent = d.online;
  el.totalOffline.textContent = d.offline;
  el.totalAtencao.textContent = d.atencao;
  el.tempoMedio.textContent = d.tempoMedioRespostaMs == null ? "--" : `${Math.round(d.tempoMedioRespostaMs)} ms`;
  el.totalAlertas.textContent = d.alertasAtivos ?? 0;
  atualizarContadorAlertasSidebar(d.alertasAtivos ?? 0);
  requestAnimationFrame(ajustarFonteCards);
}

function renderizarAlertas(alertas = []) {
  if (alertas.length === 0) {
    el.secaoAlertas.classList.add("oculto");
    return;
  }

  const podeReconhecer = podeExecutarAcao("reconhecer-alertas", sessao);
  el.secaoAlertas.classList.remove("oculto");
  el.listaAlertas.innerHTML = alertas.map((a) => {
    const classeTipo = (a.tipo || "Indisponibilidade").toLowerCase();
    const classeRec = a.reconhecido ? "reconhecido" : "";
    return `
      <div class="item-alerta ${classeTipo} ${classeRec}">
        <div>
          <div><a href="servico.html?id=${a.servicoId}"><strong>${escapar(a.mensagem)}</strong></a></div>
          <div class="meta">${tempoRelativo(a.criadoEm)} · ${a.reconhecido ? "Reconhecido" : "Pendente"}</div>
        </div>
        ${a.reconhecido || !podeReconhecer ? "" : `<button class="botao pequeno" data-reconhecer="${a.id}">✓ OK</button>`}
      </div>`;
  }).join("");

  el.listaAlertas.querySelectorAll("[data-reconhecer]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      btn.disabled = true;
      await api.reconhecerAlerta(btn.dataset.reconhecer);
      await carregar();
    });
  });
}

function renderizarTabela() {
  const busca = el.filtroBusca.value.trim().toLowerCase();
  const statusFiltro = el.filtroStatus.value;

  const filtrados = listaServicosCache.filter((s) => {
    const bateBusca = !busca
      || s.nome.toLowerCase().includes(busca)
      || s.url.toLowerCase().includes(busca);

    if (!bateBusca) return false;
    if (!statusFiltro) return true;
    if (statusFiltro === "Pausado") return !s.ativo;
    return s.ativo && s.statusAtual === statusFiltro;
  });

  if (listaServicosCache.length === 0) {
    el.tabela.innerHTML = `
      <tr><td colspan="6" class="vazio">
        Nenhum serviço cadastrado. ${admin ? '<a href="servico-form.html">Cadastre o primeiro</a>.' : ""}
      </td></tr>`;
    return;
  }

  if (filtrados.length === 0) {
    el.tabela.innerHTML = `<tr><td colspan="6" class="vazio">Nenhum serviço corresponde ao filtro selecionado.</td></tr>`;
    return;
  }

  el.tabela.innerHTML = filtrados.map((s) => `
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
    if (obterParametroUrl("acessoNegado") !== "1") {
      esconderMensagem(el.mensagem);
    }
    listaServicosCache = dados.servicos ?? [];
    renderizarResumo(dados);
    renderizarAlertas(dados.alertasRecentes ?? []);
    renderizarTabela();
    el.ultimaAtualizacao.textContent =
      `Atualizado às ${new Date().toLocaleTimeString("pt-BR")} · atualização automática a cada ${CONFIG.INTERVALO_ATUALIZACAO_MS / 1000}s`;
  } catch (erro) {
    mostrarMensagem(el.mensagem, erro.message);
  }
}

el.btnAtualizar.addEventListener("click", carregar);
el.filtroBusca.addEventListener("input", renderizarTabela);
el.filtroStatus.addEventListener("change", renderizarTabela);
el.btnReconhecerTodos.addEventListener("click", async () => {
  await api.reconhecerTodosAlertas();
  await carregar();
});

el.atalhoVerificarTodos.addEventListener("click", async () => {
  const ativos = listaServicosCache.filter((s) => s.ativo);
  if (ativos.length === 0) return;

  el.atalhoVerificarTodos.disabled = true;
  try {
    await Promise.all(ativos.map((s) => api.verificarAgora(s.id)));
    await carregar();
    mostrarMensagem(el.mensagem, `Varredura concluída em ${ativos.length} serviço(s)!`, "sucesso");
  } catch (erro) {
    mostrarMensagem(el.mensagem, erro.message);
  } finally {
    el.atalhoVerificarTodos.disabled = false;
  }
});

aplicarContextoUsuario();
carregar();
setInterval(carregar, CONFIG.INTERVALO_ATUALIZACAO_MS);
