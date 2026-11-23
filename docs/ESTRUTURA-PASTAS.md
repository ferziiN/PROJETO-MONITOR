# 📁 Estrutura de Pastas — Projeto Monitor

Documentação da organização de diretórios do projeto, baseada no [ESCOPO-PROJETO.md](./ESCOPO-PROJETO.md).

---

## Visão Geral

```
ProjetoMonitor/
│
├── README.md
├── docs/
├── imgs/
├── src/
│   ├── backend/
│   └── frontend/
└── infra/
```

---

## 📄 docs/

Documentação geral do projeto.

- `ESCOPO-PROJETO.md` → Documento de escopo completo do projeto
- `ROADMAP-DEV.md` → Roadmap prático de sprints e tarefas da equipe
- `MATRIZ-5W2H.md` → Análise 5W2H e respostas técnicas do Pitch
- `ANALISE-MONITORAMENTO-COMPLETO.md` → Estudo de viabilidade para expansão com agente de métricas (CPU, RAM e disco)
- `Documentacao_Inicial_Monitor.pdf` → Documentação inicial
- `ESTRUTURA-PASTAS.md` → Este arquivo

---

## 🖼️ imgs/

Imagens utilizadas no projeto — favicon, logotipo, imagens do front-end e demais assets visuais da identidade do projeto.

---

## 💻 src/

Código-fonte da aplicação, dividido em **back-end** e **front-end**.

---

### ⚙️ src/backend/Monitor.API/ — Back-end ASP.NET Core

Projeto principal da API REST, desenvolvido em C# com ASP.NET Core, Entity Framework Core e PostgreSQL.

| Pasta | Descrição |
|---|---|
| `Controllers/` | Endpoints da API REST (`ServicosController`, `DashboardController`, `AlertasController`, `AuthController`) |
| `Models/` | Entidades de domínio (`Servico`, `Verificacao`, `Alerta`, `Usuario`, `Enums`) |
| `DTOs/` | Data Transfer Objects — objetos de entrada/saída (`Servicos`, `Verificacoes`, `Alertas`, `Auth`, `Dashboard`) |
| `Services/` | Regras de negócio (`ServicoService`, `DashboardService`) e motor em `Services/Monitoramento/` |
| `Data/` | `MonitorDbContext` do Entity Framework Core |
| `Data/Configurations/` | Mapeamentos Fluent API (`ServicoConfiguration`, `VerificacaoConfiguration`, `AlertaConfiguration`, `UsuarioConfiguration`) |
| `Migrations/` | Migrations geradas pelo Entity Framework Core (`InitialCreate`, `AddAlertas`, `AddUsuarios`) |
| `Infra/` | Extensões de configuração de DI/pipeline (`ConfiguracaoExtensions`) e `BancoDeDadosHealthCheck` |

---

### 🎨 src/frontend/ — Front-end (HTML, CSS, JS)

Interface web do dashboard de monitoramento.

| Pasta | Descrição |
|---|---|
| `pages/` | Páginas HTML (dashboard, cadastro de serviços etc.) |
| `css/` | Arquivos de estilo CSS |
| `js/` | Scripts JavaScript (requisições à API, interatividade) |
| `assets/` | Ícones, imagens e recursos visuais do dashboard |

---

## 🏗️ infra/ — Infraestrutura e Deploy

Configurações para execução e disponibilização da aplicação em ambiente de produção.

| Pasta | Descrição |
|---|---|
| `docker/` | `Dockerfile` multi-stage da API, `docker-compose.yml` (produção/homologação) e `docker-compose.dev.yml` |
| `nginx/` | `nginx.conf` — servidor de arquivos estáticos do front-end e proxy reverso para a API |
| `scripts/` | `deploy.sh` — script automatizado de build, inicialização e verificação de saúde (`/health`) |

---

## Mapeamento Escopo → Estrutura

| Componente do Escopo | Pasta | Fase |
|---|---|---|
| API .NET / ASP.NET Core | `src/backend/Monitor.API/` | Fase 2 |
| CRUD de serviços | `Controllers/` + `Services/` | Fase 2 |
| Entidades (Serviço, Verificação) | `Models/` | Fase 2 |
| Entity Framework Core / PostgreSQL | `Data/` + `Migrations/` | Fase 2 |
| Sistema de monitoramento | `Services/` | Fase 3 |
| Dashboard | `src/frontend/` | Fase 4 |
| Docker / Nginx / Deploy | `infra/` | Fase 5 |
