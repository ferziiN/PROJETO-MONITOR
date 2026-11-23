/** Funções utilitárias compartilhadas pelas páginas. */

const ROTULOS_STATUS = {
  Online: "Online",
  Offline: "Offline",
  Atencao: "Atenção",
  Desconhecido: "Aguardando",
};

/** Escapa texto antes de inserir em HTML (evita XSS com dados vindos da API). */
export function escapar(valor) {
  return String(valor ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

/** Badge colorido de status (🟢 / 🔴 / 🟡 / cinza). */
export function badgeStatus(status) {
  const classe = (status || "Desconhecido").toLowerCase();
  return `<span class="status ${classe}">${ROTULOS_STATUS[status] ?? status}</span>`;
}

export function formatarMs(ms) {
  return ms == null ? "--" : `${ms} ms`;
}

export function formatarDataHora(iso) {
  if (!iso) return "--";
  return new Date(iso).toLocaleString("pt-BR");
}

/** "há 12s", "há 3 min"... */
export function tempoRelativo(iso) {
  if (!iso) return "nunca";
  const segundos = Math.round((Date.now() - new Date(iso).getTime()) / 1000);
  if (segundos < 5) return "agora";
  if (segundos < 60) return `há ${segundos}s`;
  if (segundos < 3600) return `há ${Math.floor(segundos / 60)} min`;
  if (segundos < 86400) return `há ${Math.floor(segundos / 3600)} h`;
  return formatarDataHora(iso);
}

export function formatarIntervalo(segundos) {
  if (segundos < 60) return `${segundos}s`;
  if (segundos < 3600) return `${Math.round(segundos / 60)} min`;
  return `${Math.round(segundos / 3600)} h`;
}

export function obterParametroUrl(nome) {
  return new URLSearchParams(window.location.search).get(nome);
}

/** Exibe uma mensagem num elemento .alerta (tipo: "erro" | "sucesso"). */
export function mostrarMensagem(elemento, texto, tipo = "erro") {
  elemento.textContent = texto;
  elemento.className = `alerta ${tipo}`;
}

export function esconderMensagem(elemento) {
  elemento.className = "alerta oculto";
}

/**
 * Reduz dinamicamente a fonte de valores/textos dentro dos cards de tamanho fixo
 * quando o conteúdo for muito extenso para caber na largura do card.
 */
export function ajustarFonteCards() {
  const seletores = [
    { sel: ".card.resumo .valor", maxPx: 26, minPx: 11 },
    { sel: ".card.resumo .rotulo", maxPx: 12, minPx: 8.5 },
    { sel: ".card-atalho .atalho-titulo", maxPx: 14.5, minPx: 10 },
    { sel: ".card-atalho .atalho-desc", maxPx: 12, minPx: 9 },
    { sel: ".detalhes dd", maxPx: 14, minPx: 9.5 },
    { sel: ".detalhes dt", maxPx: 12, minPx: 9 },
  ];

  seletores.forEach(({ sel, maxPx, minPx }) => {
    document.querySelectorAll(sel).forEach((el) => {
      // Não sobrescreve quando contém badge HTML interno (ex.: badgeStatus)
      if (el.querySelector(".status")) return;

      el.style.fontSize = `${maxPx}px`;
      let tamanho = maxPx;
      while (el.scrollWidth > el.clientWidth && tamanho > minPx) {
        tamanho -= 0.5;
        el.style.fontSize = `${tamanho}px`;
      }
    });
  });
}

window.addEventListener("resize", () => requestAnimationFrame(ajustarFonteCards));
