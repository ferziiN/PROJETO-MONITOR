namespace Monitor.API.Services.Monitoramento;

/// <summary>
/// Configurações do motor de monitoramento (seção "Monitoramento" do appsettings.json).
/// </summary>
public class MonitoramentoOptions
{
    public const string Secao = "Monitoramento";

    /// <summary>Liga/desliga as verificações automáticas em segundo plano.</summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>De quanto em quanto tempo o agendador procura serviços "vencidos".</summary>
    public int IntervaloVarreduraSegundos { get; set; } = 5;

    /// <summary>Tempo máximo de espera por uma resposta. Acima disso = Offline.</summary>
    public int TimeoutSegundos { get; set; } = 5;

    /// <summary>Tempo de resposta (ms) acima do qual o serviço é marcado como Atenção.</summary>
    public int LimiteAtencaoMs { get; set; } = 1000;

    /// <summary>Quantidade máxima de verificações executadas ao mesmo tempo.</summary>
    public int MaxVerificacoesSimultaneas { get; set; } = 10;

    /// <summary>Método HTTP utilizado na verificação ("GET" ou "HEAD").</summary>
    public string MetodoHttp { get; set; } = "GET";

    /// <summary>Por quantos dias o histórico de verificações é mantido. 0 = para sempre.</summary>
    public int RetencaoHistoricoDias { get; set; } = 30;
}

