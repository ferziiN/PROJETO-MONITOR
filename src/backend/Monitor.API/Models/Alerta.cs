namespace Monitor.API.Models;

/// <summary>
/// Registro de alerta gerado quando um serviço entra em falha/atenção ou se recupera.
/// </summary>
public class Alerta
{
    public long Id { get; set; }

    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public TipoAlerta Tipo { get; set; }
    public StatusServico StatusAnterior { get; set; }
    public StatusServico StatusNovo { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Indica se o alerta já foi reconhecido pelo operador ou resolvido automaticamente.</summary>
    public bool Reconhecido { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
}
