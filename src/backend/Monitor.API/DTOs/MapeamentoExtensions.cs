using Monitor.API.DTOs.Alertas;
using Monitor.API.DTOs.Servicos;
using Monitor.API.DTOs.Verificacoes;
using Monitor.API.Models;

namespace Monitor.API.DTOs;

/// <summary>
/// Conversões entre entidades e DTOs (mapeamento manual, sem bibliotecas extras).
/// </summary>
public static class MapeamentoExtensions
{
    public static ServicoResponse ParaResponse(this Servico s) => new(
        s.Id,
        s.Nome,
        s.Endereco,
        s.Porta,
        s.Endpoint,
        s.Tipo,
        s.ObterUrl().ToString(),
        s.IntervaloSegundos,
        s.Ativo,
        s.StatusAtual,
        s.UltimoTempoRespostaMs,
        s.UltimaVerificacaoEm,
        s.ProximaVerificacaoEm,
        s.CriadoEm,
        s.AtualizadoEm);

    public static VerificacaoResponse ParaResponse(this Verificacao v) => new(
        v.Id,
        v.ServicoId,
        v.Status,
        v.TempoRespostaMs,
        v.CodigoHttp,
        v.VerificadoEm,
        v.MensagemErro);

    public static AlertaResponse ParaResponse(this Alerta a) => new(
        a.Id,
        a.ServicoId,
        a.Servico?.Nome ?? $"Serviço #{a.ServicoId}",
        a.Servico?.ObterUrl().ToString() ?? string.Empty,
        a.Tipo,
        a.StatusAnterior,
        a.StatusNovo,
        a.Mensagem,
        a.Reconhecido,
        a.CriadoEm,
        a.ResolvidoEm);

    public static Servico ParaEntidade(this CriarServicoRequest r)
    {
        var servico = new Servico();
        r.AplicarEm(servico);
        return servico;
    }

    /// <summary>
    /// Copia os campos editáveis do request para a entidade.
    /// </summary>
    public static void AplicarEm(this ServicoRequestBase r, Servico s)
    {
        s.Nome = r.Nome.Trim();
        s.Endereco = r.Endereco.Trim().ToLowerInvariant();
        s.Porta = r.Porta;
        s.Endpoint = string.IsNullOrWhiteSpace(r.Endpoint) ? "/" : r.Endpoint.Trim();
        s.Tipo = r.Tipo;
        s.IntervaloSegundos = r.IntervaloSegundos;
        s.Ativo = r.Ativo;
    }
}
