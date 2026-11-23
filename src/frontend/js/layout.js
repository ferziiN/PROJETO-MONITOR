import {
  ehAdmin, encerrarSessao, exigirAutenticacao,
  PERMISSOES_POR_PERFIL, podeAcessarTela,
} from "./auth.js?v=4";
import { escapar } from "./utils.js?v=4";

/**
 * Injeta a Sidebar lateral retrátil com geometria fixa (sem saltos de altura ou posição
 * durante a animação de abertura/fechamento por hover).
 */
export function inicializarLayout({ telaAtiva, abaAtiva = telaAtiva }) {
  const sessao = exigirAutenticacao(telaAtiva);
  if (!sessao) return null;

  const containerSidebar = document.getElementById("sidebar-app");
  if (!containerSidebar) return sessao;

  const admin = ehAdmin(sessao);
  const infoPerfil = PERMISSOES_POR_PERFIL[sessao.perfil] ?? PERMISSOES_POR_PERFIL.Usuario;
  const iniciais = obterIniciais(sessao.nome);
  const podeCadastrar = podeAcessarTela("servico-form", sessao);

  const itensMenu = [
    {
      id: "dashboard",
      icone: "🏠",
      rotulo: "Home · Dashboard",
      href: "dashboard.html",
      liberado: true,
    },
    {
      id: "servico-form",
      icone: "➕",
      rotulo: "Cadastrar Serviço",
      href: podeCadastrar ? "servico-form.html" : "#",
      liberado: podeCadastrar,
      restritoAdmin: true,
    },
    {
      id: "api-docs",
      icone: "⚡",
      rotulo: "API Docs (Scalar)",
      href: "/scalar",
      externo: true,
      liberado: admin,
      restritoAdmin: true,
    },
  ];

  containerSidebar.innerHTML = `
    <div class="sidebar-topo">
      <a class="sidebar-item-linha marca-sidebar" href="dashboard.html" title="Monitor — Home">
        <span class="sidebar-slot-icone"><span class="pulso"></span></span>
        <span class="sidebar-colapsavel marca-titulo">MONITOR</span>
      </a>
    </div>

    <nav class="sidebar-nav">
      ${itensMenu.map((item) => {
        const ativo = abaAtiva === item.id ? "ativo" : "";
        const bloqueado = !item.liberado ? "bloqueado" : "";
        const alvo = item.externo ? 'target="_blank" rel="noopener"' : "";
        const tagRestrito = item.restritoAdmin && !admin
          ? `<span class="tag-permissao restrito">🔒</span>`
          : "";

        return `
          <a href="${item.liberado ? item.href : "#"}"
             class="sidebar-link ${ativo} ${bloqueado}"
             data-menu-id="${item.id}"
             data-liberado="${item.liberado}"
             title="${escapar(item.rotulo)}"
             ${alvo}>
            <span class="sidebar-slot-icone">${item.icone}</span>
            <span class="sidebar-texto sidebar-colapsavel">${item.rotulo}</span>
            <span class="sidebar-colapsavel">${tagRestrito}</span>
          </a>`;
      }).join("")}
    </nav>

    <div class="sidebar-rodape">
      <div class="usuario-card" title="${escapar(sessao.nome)} (${infoPerfil.rotulo})">
        <span class="sidebar-slot-icone">
          <span class="usuario-avatar ${admin ? "admin" : "usuario"}">${escapar(iniciais)}</span>
        </span>
        <div class="usuario-info sidebar-colapsavel">
          <div class="usuario-nome">${escapar(sessao.nome)}</div>
          <span class="badge-perfil ${admin ? "admin" : "usuario"}">${infoPerfil.rotulo}</span>
        </div>
      </div>

      <button class="botao btn-logout" id="btn-logout" title="Sair / Trocar conta">
        <span class="sidebar-slot-icone">⇥</span>
        <span class="sidebar-colapsavel">Sair da conta</span>
      </button>
    </div>
  `;

  // Intercepta clique em itens restritos ao ADMIN quando logado como USUÁRIO
  containerSidebar.querySelectorAll('.sidebar-link[data-liberado="false"]').forEach((link) => {
    link.addEventListener("click", (e) => {
      e.preventDefault();
      alert("Acesso restrito ao perfil ADMIN. Os acessos autorizados para o perfil USUÁRIO serão habilitados conforme definição da equipe.");
    });
  });

  document.getElementById("btn-logout")?.addEventListener("click", encerrarSessao);

  return sessao;
}

export function atualizarContadorAlertasSidebar() {
  // Mantida por compatibilidade; os alertas ficam centralizados na Home Page.
}

function obterIniciais(nome = "") {
  const partes = nome.trim().split(/\s+/);
  if (partes.length === 0) return "US";
  if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase();
  return (partes[0][0] + partes[1][0]).toUpperCase();
}
