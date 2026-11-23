import {
  ehAdmin, encerrarSessao, exigirAutenticacao,
  PERMISSOES_POR_PERFIL, podeAcessarTela,
} from "./auth.js?v=6";
import { escapar, exibirToast } from "./utils.js?v=6";

/** Ícones vetoriais SVG estilo Apple SF Symbols (monocromáticos, herdam currentColor). */
const ICONES_SF = {
  home: `<svg class="icone-svg" viewBox="0 0 24 24" aria-hidden="true"><path d="M3 10.5 12 3l9 7.5"/><path d="M5 9.5V20a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V9.5"/><path d="M9 21v-6a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v6"/></svg>`,
  plus: `<svg class="icone-svg" viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="5"/><path d="M12 8v8M8 12h8"/></svg>`,
  bolt: `<svg class="icone-svg" viewBox="0 0 24 24" aria-hidden="true"><path d="M13 2 4 14h7l-1 8 9-12h-7l1-8z"/></svg>`,
  lock: `<svg class="icone-svg pequeno" viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/></svg>`,
  logout: `<svg class="icone-svg" viewBox="0 0 24 24" aria-hidden="true"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>`,
};

/**
 * Injeta a Sidebar lateral retrátil com translucidez (macOS Vibrancy) no desktop
 * e Bottom Tab Bar estilo iOS no mobile.
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
      icone: ICONES_SF.home,
      rotulo: "Home · Dashboard",
      href: "dashboard.html",
      liberado: true,
    },
    {
      id: "servico-form",
      icone: ICONES_SF.plus,
      rotulo: "Cadastrar Serviço",
      href: podeCadastrar ? "servico-form.html" : "#",
      liberado: podeCadastrar,
      restritoAdmin: true,
    },
    {
      id: "api-docs",
      icone: ICONES_SF.bolt,
      rotulo: "API Docs (Scalar)",
      href: "/scalar",
      externo: true,
      liberado: admin,
      restritoAdmin: true,
    },
  ];

  containerSidebar.setAttribute("aria-label", "Navegação principal");
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
          ? `<span class="tag-permissao restrito" title="Exclusivo ADMIN">${ICONES_SF.lock}</span>`
          : "";

        return `
          <a href="${item.liberado ? item.href : "#"}"
             class="sidebar-link ${ativo} ${bloqueado}"
             data-menu-id="${item.id}"
             data-liberado="${item.liberado}"
             title="${escapar(item.rotulo)}"
             ${ativo ? 'aria-current="page"' : ""}
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
        <span class="sidebar-slot-icone">${ICONES_SF.logout}</span>
        <span class="sidebar-colapsavel">Sair da conta</span>
      </button>
    </div>
  `;

  // Feedback não-bloqueante via Toast quando logado como USUÁRIO e clicar em item restrito
  containerSidebar.querySelectorAll('.sidebar-link[data-liberado="false"]').forEach((link) => {
    link.addEventListener("click", (e) => {
      e.preventDefault();
      exibirToast("Acesso restrito ao perfil ADMIN.", "erro");
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
