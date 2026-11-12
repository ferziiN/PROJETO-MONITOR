# 📊 Análise de Expansão de Escopo: Monitoramento Completo (CPU, Memória e Disco)

> **Documento de Apoio à Decisão Técnica**  
> **Origem:** Feedback recebido durante o Pitch do projeto com o **Prof. Glender**  
> **Objetivo:** Avaliar vantagens, desvantagens, impacto arquitetural e estratégia de viabilização para transformar o Projeto Monitor em uma plataforma completa de observabilidade de infraestrutura.

---

## 1. Contexto e Motivação

Durante a apresentação do Pitch para o **Prof. Glender**, foi sugerido incrementar a robustez da solução, indo além do teste de disponibilidade sintética (HTTP/HTTPS) e incorporando o acompanhamento de métricas do sistema operacional dos servidores (CPU, memória RAM, armazenamento em disco e processos).

Este documento formaliza o estudo de viabilidade técnica para que a equipe possa avaliar os impactos antes de alterar o [ROADMAP-DEV.md](./ROADMAP-DEV.md).

---

## 2. Comparativo de Modelos: Sintético vs. Completo (Agente)

| Dimensão | Modelo Atual (Sintético / Black-box) | Modelo Proposto (Completo / White-box) |
|---|---|---|
| **Ponto de observação** | De fora para dentro (via requisições de rede) | De dentro para fora (via telemetria no servidor) |
| **Métricas coletadas** | Status (*Online / Offline*), tempo de resposta (ms), código HTTP | Status, latência, uso de CPU (%), consumo de RAM (%), ocupação de disco (%) |
| **Instalação no alvo** | Nenhuma (basta cadastrar a URL/IP) | Exige um script leve ou mini-container no servidor alvo |
| **Capacidade de alerta** | Reativo (alerta apenas **após** a queda do serviço) | Preventivo (alerta em estado de **Atenção** antes da queda) |
| **Valor de mercado** | Uptime checker básico (equivalente ao UptimeRobot) | Plataforma de observabilidade (linha Datadog / Zabbix) |

---

## 3. Vantagens (Por que adotar?)

### 🏆 1. Alto Impacto e Alinhamento Acadêmico (A3)
* Eleva substancialmente o nível técnico do projeto perante os professores e avaliadores.
* Atende integralmente à persona **Carlos Eduardo (Gestor de TI)** já detalhada no [README.md](../README.md), cuja dor explícita é *"a falta de visibilidade centralizada sobre consumo de recursos (como CPU e memória) em múltiplos servidores"*.

### 🔮 2. Diagnóstico Preditivo e Proativo
* Em um modelo puramente HTTP, a equipe só descobre um incidente quando a aplicação cai (erro 500 ou timeout).
* Com telemetria de hardware, o sistema emite avisos de **Atenção (Amarelo)** quando o consumo de RAM atinge 90% ou o disco está em 98%, permitindo ação antes do serviço colapsar.

### 📈 3. Dashboard Rico e Visualmente Impressionante
* Permite gráficos em linha temporal com Chart.js ou ApexCharts (curvas de uso de CPU ao longo do dia).
* Exibição de medidores visuais (*gauges* circulares, barras de progresso coloridas de disco e memória), tornando a interface web moderna e atrativa.

### 🤝 4. Divisão Natural e Eficiente entre as Frentes de Trabalho
* **Equipe de Infraestrutura:** Assume a criação do script de coleta, testes em containers Docker/Linux e definição das faixas aceitáveis de consumo de hardware.
* **Equipe de Desenvolvimento:** Modela os contratos da API (`POST /api/metricas`), persistência no banco e componentes dinâmicos do front-end.

---

## 4. Desvantagens e Desafios Técnicos

### ⚠️ 1. Aumento da Superfície de Software
* Adiciona um novo elemento ao ecossistema: o **Agente Coletor**.
* Exige mecanismos de autenticação de servidores (ex.: cada servidor cadastrado recebe um `Token` ou `API Key` para enviar suas métricas com segurança à API do Monitor).

### 💾 2. Volume de Dados no PostgreSQL (Time-Series)
* Se 5 servidores enviarem métricas a cada 30 segundos, são **~14.400 registros diários** adicionados ao banco de dados.
* Exige modelagem consciente, índices adequados em chaves de data/hora e uma política simples de retenção de dados (ex.: manter métricas detalhadas apenas dos últimos 7 dias).

### ⚙️ 3. Atrito de Configuração no Alvo (Onboarding)
* O usuário não pode apenas preencher um formulário com a URL; ele precisará rodar um comando (script ou container) no servidor que deseja monitorar.

---

## 5. Estratégia de Viabilização (Sem Estourar Prazos)

Para colher os benefícios da robustez exigida no pitch sem comprometer o cronograma acadêmico, a recomendação é uma **implementação minimalista e híbrida**:

```text
┌────────────────────────────────────────────────────────┐
│                   Servidor Monitorado                  │
│                                                        │
│   ┌────────────────────────────────────────────────┐   │
│   │    Mini-Agente (Script Bash ou PowerShell)     │   │
│   │  Lê CPU, RAM e Disco a cada 30s                │   │
│   │  Envia via curl/Invoke-RestMethod com Token    │   │
│   └───────────────────────┬────────────────────────┘   │
└───────────────────────────┼────────────────────────────┘
                            │
                     POST /api/metricas
                     (JSON com métricas)
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│                   Servidor do Monitor                  │
│                                                        │
│   ┌───────────────────┐        ┌───────────────────┐   │
│   │     API .NET      ├───────►│    PostgreSQL     │   │
│   │   ASP.NET Core    │        │  (Tabela Metricas)│   │
│   └─────────┬─────────┘        └───────────────────┘   │
│             │                                          │
│             ▼                                          │
│   ┌───────────────────┐                                │
│   │     Dashboard     │                                │
│   │ Gráficos & Gauges │                                │
│   └───────────────────┘                                │
└────────────────────────────────────────────────────────┘
```

1. **Agente sem complicação:** Em vez de compilar um binário complexo, criar um script Bash (para Linux) ou PowerShell (para Windows) de ~30 linhas que lê comandos nativos (`free`, `df`, `top`) e envia via `curl`.
2. **Modelo Híbrido:** O sistema continua fazendo a checagem HTTP básica (para quem só quer status rápido), mas ganha uma aba de "Métricas Avançadas" para servidores com o agente instalado.
3. **Persistência Enxuta:** Gravar a última leitura em tempo real e uma tabela simples com histórico recente para renderizar gráficos de 24h.

---

## 6. Próxima Etapa (Após Leitura e Decisão)

Quando você e a equipe estiverem prontos para retomar:

* **Se APROVADO:** Atualizaremos o [ROADMAP-DEV.md](./ROADMAP-DEV.md) reorganizando as tarefas e inserindo a Sprint do Módulo de Métricas e Mini-Agente.
* **Se REJEITADO ou POSTERGADO:** Manteremos o foco 100% no MVP sintético original de HTTP/HTTPS conforme planejado inicialmente.
