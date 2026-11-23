using Monitor.API.DTOs.Alertas;
using Monitor.API.DTOs.Servicos;

namespace Monitor.API.DTOs.Dashboard;

/// <summary>
/// Resumo geral exibido no dashboard (GET /api/dashboard).
/// </summary>
public record DashboardResponse(
    int TotalServicos,
    int Ativos,
    int Inativos,
    int Online,
    int Offline,
    int Atencao,
    int Desconhecido,
    int AlertasAtivos,
    double? TempoMedioRespostaMs,
    DateTime GeradoEm,
    IReadOnlyList<ServicoResponse> Servicos,
    IReadOnlyList<AlertaResponse> AlertasRecentes);
