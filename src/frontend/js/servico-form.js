import { api } from "./api.js?v=6";
import { inicializarLayout } from "./layout.js?v=6";
import { esconderMensagem, mostrarMensagem, obterParametroUrl } from "./utils.js?v=6";

inicializarLayout({ telaAtiva: "servico-form" });

/** Cadastro (sem ?id) e edição (com ?id=123) de serviços. */
const id = obterParametroUrl("id");
const editando = Boolean(id);

const form = document.getElementById("form-servico");
const mensagem = document.getElementById("mensagem");
const btnSalvar = document.getElementById("btn-salvar");
const previaUrl = document.getElementById("previa-url");

function lerFormulario() {
  const f = form.elements;
  return {
    nome: f.nome.value.trim(),
    tipo: f.tipo.value,
    endereco: f.endereco.value.trim(),
    porta: f.porta.value ? Number(f.porta.value) : null,
    endpoint: f.endpoint.value.trim() || "/",
    intervaloSegundos: Number(f.intervaloSegundos.value),
    ativo: f.ativo.checked,
  };
}

function preencherFormulario(s) {
  const f = form.elements;
  f.nome.value = s.nome;
  f.tipo.value = s.tipo;
  f.endereco.value = s.endereco;
  f.porta.value = s.porta ?? "";
  f.endpoint.value = s.endpoint;
  f.intervaloSegundos.value = s.intervaloSegundos;
  f.ativo.checked = s.ativo;
  atualizarPrevia();
}

function atualizarPrevia() {
  const d = lerFormulario();
  const protocolo = d.tipo === "Http" ? "http" : "https";
  const porta = d.porta ? `:${d.porta}` : "";
  const endpoint = d.endpoint.startsWith("/") ? d.endpoint : `/${d.endpoint}`;
  previaUrl.textContent = `${protocolo}://${d.endereco || "…"}${porta}${endpoint}`;
}

/** Validação simples no cliente (a API valida de novo). Retorna { Campo: "mensagem" }. */
function validar(d) {
  const erros = {};
  if (d.nome.length < 2) erros.Nome = "Informe um nome com pelo menos 2 caracteres.";
  if (!d.endereco) erros.Endereco = "Informe o IP ou domínio.";
  else if (/^https?:\/\//i.test(d.endereco) || /[/:\s]/.test(d.endereco))
    erros.Endereco = "Informe apenas o domínio/IP, sem 'http://', porta ou caminho.";
  if (d.porta !== null && (d.porta < 1 || d.porta > 65535)) erros.Porta = "Porta entre 1 e 65535.";
  if (!d.endpoint.startsWith("/")) erros.Endpoint = "O endpoint deve começar com '/'.";
  if (!(d.intervaloSegundos >= 10 && d.intervaloSegundos <= 86400))
    erros.IntervaloSegundos = "Intervalo entre 10 e 86400 segundos.";
  return erros;
}

function exibirErrosCampos(erros) {
  form.querySelectorAll("[data-erro]").forEach((span) => {
    const chave = span.dataset.erro;
    // A API retorna as chaves como "Nome" ou "$.nome" dependendo do tipo do erro.
    const msg = erros[chave] ?? erros[chave.charAt(0).toLowerCase() + chave.slice(1)];
    span.textContent = Array.isArray(msg) ? msg.join(" ") : (msg ?? "");
  });
}

async function salvar(evento) {
  evento.preventDefault();
  esconderMensagem(mensagem);

  const dados = lerFormulario();
  const erros = validar(dados);
  exibirErrosCampos(erros);
  if (Object.keys(erros).length > 0) return;

  btnSalvar.disabled = true;
  try {
    const servico = editando
      ? await api.atualizarServico(id, dados)
      : await api.criarServico(dados);
    window.location.href = `servico.html?id=${servico.id}`;
  } catch (erro) {
    exibirErrosCampos(erro.errosCampos ?? {});
    mostrarMensagem(mensagem, erro.status === 400 ? "Verifique os campos destacados." : erro.message);
  } finally {
    btnSalvar.disabled = false;
  }
}

async function iniciar() {
  form.addEventListener("input", atualizarPrevia);
  form.addEventListener("submit", salvar);
  atualizarPrevia();

  if (!editando) return;

  document.getElementById("titulo").textContent = "Editar serviço";
  document.title = "Editar serviço · Monitor";
  document.getElementById("btn-voltar").href = `servico.html?id=${id}`;
  document.getElementById("btn-cancelar").href = `servico.html?id=${id}`;

  try {
    preencherFormulario(await api.obterServico(id));
  } catch (erro) {
    mostrarMensagem(mensagem, erro.message);
    btnSalvar.disabled = true;
  }
}

iniciar();

