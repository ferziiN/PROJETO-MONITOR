import { obterSessao, realizarLogin } from "./auth.js?v=4";
import { esconderMensagem, mostrarMensagem } from "./utils.js?v=4";

// Se já estiver autenticado, vai direto para a Home/Dashboard
if (obterSessao()) {
  window.location.replace("dashboard.html");
}

const form = document.getElementById("form-login");
const inputLogin = document.getElementById("login");
const inputSenha = document.getElementById("senha");
const btnEntrar = document.getElementById("btn-entrar");
const mensagem = document.getElementById("mensagem");
const botoesDemo = document.querySelectorAll(".card-perfil-demo");

botoesDemo.forEach((btn) => {
  btn.addEventListener("click", () => {
    botoesDemo.forEach((b) => b.classList.remove("ativo"));
    btn.classList.add("ativo");
    inputLogin.value = btn.dataset.login;
    inputSenha.value = btn.dataset.senha;
    esconderMensagem(mensagem);
  });
});

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  esconderMensagem(mensagem);

  const login = inputLogin.value.trim();
  const senha = inputSenha.value;

  if (!login || !senha) {
    mostrarMensagem(mensagem, "Preencha o usuário e a senha para entrar.");
    return;
  }

  btnEntrar.disabled = true;
  btnEntrar.textContent = "Autenticando…";

  try {
    await realizarLogin(login, senha);
    window.location.href = "dashboard.html";
  } catch (erro) {
    mostrarMensagem(mensagem, erro.message || "Falha ao autenticar.");
  } finally {
    btnEntrar.disabled = false;
    btnEntrar.textContent = "Entrar no Monitor →";
  }
});
