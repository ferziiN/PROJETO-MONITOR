# 🗺️ ROADMAP DE DESENVOLVIMENTO — Projeto Monitor

> ROADMAP prático de desenvolvimento para nossa equipe, com a ordem de implementação, dependências entre tarefas e marcos de entrega.
> Baseado no [ESCOPO-PROJETO.md](./ESCOPO-PROJETO.md), [ESTRUTURA-PASTAS.md](./ESTRUTURA-PASTAS.md) e [README.md](../README.md).

---

## 📍 Estado Atual do Projeto

| Item | Status |
|---|---|
| Documentação de escopo | ✅ Concluída |
| Estrutura de pastas | ✅ Criada |
| Repositório Git / GitHub | ✅ Configurado |
| Código back-end | ✅ Concluído (Sprints 1, 2 e 5 — CRUD, Monitoramento, Métricas e Alertas) |
| Código front-end | ✅ Concluído (Sprints 3 e 5 — Dashboard, Filtros, Alertas e Gráficos) |
| Infraestrutura (Docker/Nginx) | ✅ Configurada (Sprint 4 — `Dockerfile`, `docker-compose.yml`, `nginx.conf` e `deploy.sh`) |

> As Fases 1 a 5 do roadmap estão implementadas e prontas para execução local (SQLite/PostgreSQL) ou via Docker Compose.

---

## 🧭 Estratégia Geral

```text
SPRINT 1          SPRINT 2          SPRINT 3          SPRINT 4          SPRINT 5
─────────         ─────────         ─────────         ─────────         ─────────
 Fundação       Monitoramento       Dashboard        Infra/Deploy     Histórico &
 Back-end         Automático       Front-end         Containerização    Alertas
```

A ideia central é: **construir de dentro pra fora**. Primeiro o coração do sistema (API + banco), depois o motor de monitoramento, em seguida a interface visual, depois o deploy real, e por último os indicadores avançados.

---

## 🏗️ SPRINT 1 — Fundação do Back-end

**Objetivo:** Ter a API rodando com CRUD completo de serviços e banco de dados funcionando.

**Duração estimada:** 1–2 semanas

### Por onde começar (ordem exata):

#### 1.1 — Inicializar o projeto ASP.NET Core
- [✅] Criar o projeto `webapi` dentro de `src/backend/Monitor.API/`
- [✅] Configurar o `.gitignore` adequado para .NET
- [✅] Validar que o projeto compila e roda (`dotnet run`)

#### 1.2 — Configurar o banco de dados
- [✅] Configurar suporte híbrido a banco de dados (SQLite local out-of-the-box em dev + PostgreSQL via `infra/docker/docker-compose.dev.yml` ou produção)
- [✅] Adicionar pacotes NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`
- [✅] Criar a connection string no `appsettings.Development.json`
- [✅] Criar o `MonitorDbContext` em `Data/`

#### 1.3 — Modelar as entidades
- [✅] Criar a entidade `Servico` em `Models/` com os campos:
  - `Id` (Guid ou int)
  - `Nome`
  - `Endereco` (IP ou domínio)
  - `Porta`
  - `Endpoint`
  - `Tipo` (HTTP / HTTPS)
  - `IntervaloSegundos`
  - `Ativo` (bool)
  - `CriadoEm`, `AtualizadoEm`
- [✅] Criar a entidade `Verificacao` em `Models/`:
  - `Id`
  - `ServicoId` (FK)
  - `Status` (Online / Offline / Atencao)
  - `TempoRespostaMs`
  - `VerificadoEm`
  - `MensagemErro` (nullable)
- [✅] Criar as configurações Fluent API em `Data/Configurations/`

#### 1.4 — Criar Migrations e aplicar
- [✅] Gerar migrations (`InitialCreate` e `AddAlertas`)
- [✅] Aplicar esquema automaticamente ao iniciar a API (`EnsureCreatedAsync` para SQLite / `MigrateAsync` para PostgreSQL)

#### 1.5 — Criar DTOs
- [✅] `CriarServicoRequest`
- [✅] `AtualizarServicoRequest`
- [✅] `ServicoResponse`
- [✅] `VerificacaoResponse`

#### 1.6 — Criar o CRUD de Serviços
- [✅] Criar `IServicoService` e `ServicoService` em `Services/`
- [✅] Criar `ServicosController` em `Controllers/`
  - `POST /api/servicos` — Cadastrar
  - `GET /api/servicos` — Listar todos
  - `GET /api/servicos/{id}` — Buscar por ID
  - `PUT /api/servicos/{id}` — Atualizar
  - `DELETE /api/servicos/{id}` — Remover

### ✅ Marco da Sprint 1
> **API rodando localmente com CRUD completo. É possível cadastrar, listar, editar e deletar serviços via Postman/Insomnia.**

---

## ⚙️ SPRINT 2 — Motor de Monitoramento

**Objetivo:** O sistema realiza verificações automáticas e periódicas de disponibilidade dos serviços cadastrados.

**Duração estimada:** 1–2 semanas

**Depende de:** Sprint 1 concluída

#### 2.1 — Criar o serviço de verificação
- [✅] Criar `IMonitoramentoService` e `MonitoramentoService` em `Services/`
- [✅] Implementar a lógica de requisição HTTP/HTTPS ao endereço do serviço (`VerificadorHttp`)
- [✅] Definir timeout configurável
- [✅] Tratar respostas:
  - Resposta com sucesso (2xx) → **Online**
  - Timeout ou erro de conexão → **Offline**
  - Resposta lenta (acima de threshold) → **Atenção** (opcional nesta fase)
- [✅] Registrar tempo de resposta em milissegundos
- [✅] Salvar o resultado como `Verificacao` no banco

#### 2.2 — Implementar verificações periódicas
- [✅] Criar um `BackgroundService` (Hosted Service — `MonitoramentoWorker`) que:
  - Busca todos os serviços ativos
  - Executa a verificação de cada serviço no intervalo configurado
  - Roda de forma assíncrona e não bloqueia a API
- [✅] Implementar lógica para respeitar o intervalo individual de cada serviço

#### 2.3 — Criar endpoints de consulta de status
- [✅] `GET /api/servicos/{id}/status` — Status atual (última verificação)
- [✅] `GET /api/servicos/{id}/verificacoes` — Histórico de verificações
- [✅] `GET /api/dashboard` — Resumo geral (total de serviços, online, offline)

### ✅ Marco da Sprint 2
> **O sistema monitora automaticamente os serviços cadastrados e registra resultados no banco. É possível consultar o status via API.**

---

## 🎨 SPRINT 3 — Dashboard Front-end

**Objetivo:** Interface web funcional que consome a API e exibe o estado dos serviços.

**Duração estimada:** 1–2 semanas

**Depende de:** Sprint 2 concluída (API com dados reais)

#### 3.1 — Estrutura base do front-end e Layout Centralizado
- [✅] Criar `index.html` na raiz de `src/frontend/` (redirecionamento inteligente por sessão)
- [✅] Criar os arquivos CSS base em `css/` (reset, variáveis, layout com Sidebar, login e responsividade)
- [✅] Criar os módulos JS principais em `js/` (`api.js`, `auth.js`, `layout.js`, `config.js`, `utils.js`)
- [✅] Implementar **Abas Laterais (Sidebar)** responsiva para separar e centralizar a navegação entre as telas

#### 3.2 — Home Responsiva & Página do Dashboard
- [✅] Criar Hero Banner moderno com saudação do usuário autenticado, badge de perfil (`ADMIN` / `USUÁRIO`) e estado geral
- [✅] Criar grade de **Atalhos Rápidos** (Cadastrar Serviço, Varredura Imediata, Central de Alertas e Frota Monitorada)
- [✅] Exibir contadores de resumo (total, online, offline, atenção, latência média e alertas ativos)
- [✅] Listar serviços monitorados em tabela com busca, filtro por status e indicadores coloridos (🟢🔴🟡)
- [✅] Atualização automática dos dados (polling a cada X segundos)

#### 3.3 — Autenticação e Controle de Acesso por Perfil (ADMIN vs USUÁRIO)
- [✅] Criar tela de **Login** estilizada (`pages/login.html` + `js/login.js`) no padrão visual dark da plataforma
- [✅] Criar endpoint de autenticação (`POST /api/auth/login`) e tabela `usuarios` com perfis `Admin` e `Usuario`
- [✅] Garantir que o **Login ADMIN** tenha acesso irrestrito a todas as telas e ações
- [✅] Estruturar matriz de permissões configurável (`PERMISSOES_POR_PERFIL` em `js/auth.js`) para o **Login USUÁRIO**, permitindo decidir e ajustar os acessos autorizados posteriormente

#### 3.4 — Página de cadastro de serviços (Restrita ao perfil ADMIN)
- [✅] Formulário para criar/editar serviço com prévia de URL em tempo real
- [✅] Validação básica dos campos
- [✅] Feedback visual de sucesso/erro

#### 3.5 — Página de detalhes do serviço
- [✅] Exibir informações completas do serviço
- [✅] Listar histórico recente de verificações e gráficos de latência/uptime
- [✅] Botões administrativos (ativar/desativar, editar, excluir) controlados pelo perfil do usuário

#### 3.6 — Configurar CORS na API
- [✅] Habilitar CORS no back-end para permitir chamadas do front-end
- [✅] Servir arquivos estáticos (se necessário durante dev)

### ✅ Marco da Sprint 3
> **Dashboard visual funcionando. Usuário consegue ver status dos serviços, cadastrar novos serviços e visualizar detalhes, tudo pelo navegador.**

---

## 🐳 SPRINT 4 — Infraestrutura e Deploy

**Objetivo:** Aplicação containerizada e acessível em um ambiente real.

**Duração estimada:** 1–2 semanas

**Depende de:** Sprint 3 concluída (aplicação funcional de ponta a ponta)

#### 4.1 — Dockerizar a aplicação
- [✅] Criar `Dockerfile` para a API .NET em `infra/docker/`
- [✅] Criar `docker-compose.yml` com:
  - Container da API
  - Container do PostgreSQL
  - Volumes para persistência do banco
  - Rede interna entre containers
- [ ] Validar que `docker-compose up` sobe tudo corretamente no servidor alvo

#### 4.2 — Configurar Nginx
- [✅] Criar configuração do Nginx em `infra/nginx/nginx.conf`
- [✅] Configurar como proxy reverso para a API
- [✅] Servir os arquivos estáticos do front-end via Nginx
- [✅] Adicionar Nginx ao `docker-compose.yml`

#### 4.3 — Preparar servidor Linux
- [ ] Provisionar servidor (VPS, máquina virtual, etc.)
- [ ] Instalar Docker e Docker Compose
- [ ] Configurar firewall e portas necessárias
- [ ] Configurar acesso SSH

#### 4.4 — Deploy
- [✅] Criar script de deploy em `infra/scripts/deploy.sh`
- [ ] Realizar o primeiro deploy no servidor
- [ ] Validar a aplicação acessível via browser
- [ ] Documentar o processo de deploy

### ✅ Marco da Sprint 4
> **Aplicação rodando em um servidor Linux real, acessível pela internet (ou rede local), com Docker e Nginx.**

---

## 📊 SPRINT 5 — Histórico, Indicadores e Alertas

**Objetivo:** Adicionar camada analítica ao monitoramento e sistema de alertas.

**Duração estimada:** 2–3 semanas

**Depende de:** Sprint 4 concluída (aplicação em produção)

#### 5.1 — Histórico e métricas
- [✅] Implementar cálculo de disponibilidade (% uptime)
- [✅] Implementar cálculo de tempo médio de resposta
- [✅] Identificar e registrar períodos de indisponibilidade
- [✅] Criar endpoints para consulta de métricas
  - `GET /api/servicos/{id}/metricas?periodo=24h`

#### 5.2 — Gráficos no dashboard
- [✅] Adicionar barra visual de disponibilidade ao longo do tempo
- [✅] Adicionar gráfico SVG interativo de tempo de resposta
- [✅] Seletor de período (`1h`, `24h`, `7d`, `30d`) na tela de detalhes

#### 5.3 — Sistema de alertas
- [✅] Detectar quando serviço muda para `Offline` ou `Atencao` (e resolver automaticamente ao voltar para `Online`)
- [✅] Criar registro de alerta no banco (`Alerta` + `AlertasController`)
- [✅] Exibir alertas no dashboard (card contador + lista de alertas com botão de reconhecimento)
- [✅] Definir regras configuráveis de timeout e limite de atenção (`MonitoramentoOptions`)

#### 5.4 — Notificações (opcional/futuro)
- [ ] Avaliar integração com e-mail ou webhook
- [ ] Implementar envio de notificação quando alerta é criado

### ✅ Marco da Sprint 5
> **Dashboard com gráficos, métricas de disponibilidade e sistema de alertas funcional.**

---

## 🔮 Pós-MVP — Evolução Futura

Estas funcionalidades **não são prioridade agora**, mas devem ser consideradas para versões futuras:

| Funcionalidade | Prioridade | Complexidade | Status |
|---|---|---|---|
| Autenticação, Login e Perfis (`ADMIN` / `USUÁRIO`) | 🔴 Alta | Média | ✅ Implementado (permissões finas do `USUÁRIO` configuráveis) |
| Agente de monitoramento (CPU, RAM, disco) | 🟡 Média | Alta | 📋 Analisado em `ANALISE-MONITORAMENTO-COMPLETO.md` |
| Monitoramento de containers Docker | 🟡 Média | Média | ⏳ Futuro |
| CI/CD (deploy automatizado) | 🟡 Média | Média | ⏳ Futuro |
| Refinamento de permissões do perfil `USUÁRIO` | 🟡 Média | Baixa | 🔄 A definir pela equipe |
| Centralização de logs | 🟢 Baixa | Alta | ⏳ Futuro |
| Monitoramento de múltiplos ambientes | 🟢 Baixa | Alta | ⏳ Futuro |

---

## 📋 Resumo da Timeline

```text
                        SPRINT 1          SPRINT 2          SPRINT 3          SPRINT 4          SPRINT 5
                       (1-2 sem)         (1-2 sem)         (1-2 sem)         (1-2 sem)         (2-3 sem)
                     ┌───────────┐     ┌───────────┐     ┌───────────┐     ┌───────────┐     ┌───────────┐
                     │ Fundação  │     │   Motor   │     │ Dashboard │     │  Infra &  │     │ Histórico │
                     │ Back-end  │────▶│   Monit.  │────▶│ Front-end │────▶│  Deploy   │────▶│ & Alertas │
                     │           │     │           │     │           │     │           │     │           │
                     │ • Projeto │     │ • HTTP    │     │ • HTML/   │     │ • Docker  │     │ • Métricas│
                     │ • EF Core │     │ • BackSvc │     │   CSS/JS  │     │ • Nginx   │     │ • Gráficos│
                     │ • CRUD    │     │ • Status  │     │ • Cards   │     │ • Linux   │     │ • Alertas │
                     │ • Postgres│     │ • Timeout │     │ • Forms   │     │ • Script  │     │ • Notif.  │
                     └───────────┘     └───────────┘     └───────────┘     └───────────┘     └───────────┘
                          │                 │                 │                 │                  │
                          ▼                 ▼                 ▼                 ▼                  ▼
                     API + CRUD        Monitoramento      Interface        App em             Dashboard
                      funcional         automático         visual         produção            completo
```

**Tempo total estimado: 6 a 11 semanas** (dependendo da disponibilidade da equipe)

---

## 💡 Dicas para o Início

1. **Comece pelo `dotnet new webapi`** — não perca tempo configurando ferramentas, comece a codar logo.
2. **Use Docker Compose para o PostgreSQL desde o dia 1** — evita instalar o banco na máquina e padroniza o ambiente de dev.
3. **Teste a API pelo Postman/Insomnia** antes de começar o front-end — garante que o back-end funciona isoladamente.
4. **Faça commits pequenos e frequentes** — facilita rastrear problemas e manter o histórico limpo.
5. **Não pule para o front-end antes de ter a Sprint 2 concluída** — o dashboard precisa de dados reais para fazer sentido.

---

> 📅 **Documento criado em:** 28/09/2026
> 📄 **Baseado em:** ESCOPO-PROJETO.md, ESTRUTURA-PASTAS.md, README.md
