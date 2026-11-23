import { api } from "./api.js?v=4";

const CHAVE_SESSAO = "monitor.sessao";

/**
 * Matriz centralizada de permissões por perfil.
 * - ADMIN: acesso irrestrito ("*") a todas as telas e ações.
 * - USUARIO: lista configurável de telas/ações autorizadas (fácil de ajustar conforme decisão da equipe).
 */
export const PERMISSOES_POR_PERFIL = {
  Admin: {
    rotulo: "ADMIN",
    descricao: "Acesso total a todas as telas, configurações e cadastros",
    telas: ["*"],
    acoes: ["*"],
  },
  Usuario: {
    rotulo: "USUÁRIO",
    descricao: "Acesso de visualização e acompanhamento (permissões de escrita restritas ao Admin)",
    // Ajuste aqui futuramente quais telas e ações o perfil USUÁRIO poderá acessar:
    telas: ["dashboard", "servicos", "alertas", "servico-detalhe"],
    acoes: ["verificar-agora"],
  },
};

const CONTAS_FALLBACK_OFFLINE = {
  admin: {
    id: 1,
    nome: "Carlos Eduardo (Admin TI)",
    login: "admin",
    senha: "admin123",
    perfil: "Admin",
    telasAutorizadas: ["*"],
  },
  usuario: {
    id: 2,
    nome: "Operador NOC (Usuário)",
    login: "usuario",
    senha: "usuario123",
    perfil: "Usuario",
    telasAutorizadas: PERMISSOES_POR_PERFIL.Usuario.telas,
  },
};

export function obterSessao() {
  try {
    const raw = localStorage.getItem(CHAVE_SESSAO);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function salvarSessao(dados) {
  localStorage.setItem(CHAVE_SESSAO, JSON.stringify(dados));
}

export function encerrarSessao() {
  localStorage.removeItem(CHAVE_SESSAO);
  window.location.href = "login.html";
}

export function ehAdmin(sessao = obterSessao()) {
  return sessao?.perfil === "Admin";
}

export function podeAcessarTela(idTela, sessao = obterSessao()) {
  if (!sessao) return false;
  if (sessao.perfil === "Admin") return true;
  const regras = PERMISSOES_POR_PERFIL[sessao.perfil] ?? PERMISSOES_POR_PERFIL.Usuario;
  return regras.telas.includes("*") || regras.telas.includes(idTela);
}

export function podeExecutarAcao(idAcao, sessao = obterSessao()) {
  if (!sessao) return false;
  if (sessao.perfil === "Admin") return true;
  const regras = PERMISSOES_POR_PERFIL[sessao.perfil] ?? PERMISSOES_POR_PERFIL.Usuario;
  return regras.acoes.includes("*") || regras.acoes.includes(idAcao);
}

/**
 * Garante que o usuário está logado e autorizado a abrir a tela atual.
 */
export function exigirAutenticacao(idTela) {
  const sessao = obterSessao();
  if (!sessao) {
    window.location.replace("login.html");
    return null;
  }

  if (idTela && !podeAcessarTela(idTela, sessao)) {
    window.location.replace("dashboard.html?acessoNegado=1");
    return null;
  }

  return sessao;
}

/**
 * Autentica contra `POST /api/auth/login` ou usa fallback local caso apenas o front-end esteja aberto.
 */
export async function realizarLogin(login, senha) {
  const loginLimpo = login.trim().toLowerCase();
  try {
    const resposta = await api.login({ login: loginLimpo, senha });
    salvarSessao(resposta);
    return resposta;
  } catch (erro) {
    // Se a API não estiver rodando (status 0), permite testar o front-end com as contas padrão
    if (erro.status === 0) {
      const conta = CONTAS_FALLBACK_OFFLINE[loginLimpo];
      if (conta && conta.senha === senha) {
        const sessaoLocal = {
          id: conta.id,
          nome: conta.nome,
          login: conta.login,
          perfil: conta.perfil,
          token: "local-demo-token",
          autenticadoEm: new Date().toISOString(),
          telasAutorizadas: conta.telasAutorizadas,
        };
        salvarSessao(sessaoLocal);
        return sessaoLocal;
      }
    }
    throw erro;
  }
}
