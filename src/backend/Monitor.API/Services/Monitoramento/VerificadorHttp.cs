using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Monitor.API.Models;

namespace Monitor.API.Services.Monitoramento;

/// <summary>
/// Resultado bruto de uma requisição de verificação (ainda não persistido).
/// </summary>
public record ResultadoVerificacao(
    StatusServico Status,
    int? TempoRespostaMs,
    int? CodigoHttp,
    string? MensagemErro);

public interface IVerificadorHttp
{
    Task<ResultadoVerificacao> VerificarAsync(Uri url, CancellationToken ct);
}

/// <summary>
/// Executa a requisição HTTP/HTTPS e classifica o resultado em Online / Offline / Atenção.
/// Não acessa o banco — apenas a rede.
/// </summary>
public class VerificadorHttp(
    IHttpClientFactory httpClientFactory,
    IOptions<MonitoramentoOptions> options) : IVerificadorHttp
{
    public const string NomeHttpClient = "monitoramento";

    private readonly MonitoramentoOptions _opcoes = options.Value;

    public async Task<ResultadoVerificacao> VerificarAsync(Uri url, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(NomeHttpClient);
        var metodo = string.Equals(_opcoes.MetodoHttp, "HEAD", StringComparison.OrdinalIgnoreCase)
            ? HttpMethod.Head
            : HttpMethod.Get;

        using var request = new HttpRequestMessage(metodo, url);

        // Timeout por verificação (independente do token de encerramento da aplicação).
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_opcoes.TimeoutSegundos));

        var cronometro = Stopwatch.StartNew();

        try
        {
            // ResponseHeadersRead: não baixa o corpo da resposta, só os cabeçalhos (verificação leve).
            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            cronometro.Stop();
            var tempoMs = (int)cronometro.ElapsedMilliseconds;
            var codigo = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return new ResultadoVerificacao(
                    StatusServico.Offline, tempoMs, codigo,
                    $"HTTP {codigo} {response.ReasonPhrase}".Trim());
            }

            var status = tempoMs > _opcoes.LimiteAtencaoMs ? StatusServico.Atencao : StatusServico.Online;
            var mensagem = status == StatusServico.Atencao
                ? $"Tempo de resposta acima do limite ({_opcoes.LimiteAtencaoMs} ms)."
                : null;

            return new ResultadoVerificacao(status, tempoMs, codigo, mensagem);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ResultadoVerificacao(
                StatusServico.Offline, null, null,
                $"Tempo limite excedido ({_opcoes.TimeoutSegundos}s).");
        }
        catch (HttpRequestException ex)
        {
            return new ResultadoVerificacao(StatusServico.Offline, null, null, TraduzirErro(ex));
        }
    }

    private static string TraduzirErro(HttpRequestException ex) => ex.HttpRequestError switch
    {
        HttpRequestError.NameResolutionError => "Não foi possível resolver o endereço (DNS).",
        HttpRequestError.ConnectionError => "Falha de conexão (serviço inacessível ou porta fechada).",
        HttpRequestError.SecureConnectionError => "Falha na conexão segura (certificado SSL/TLS inválido).",
        HttpRequestError.InvalidResponse => "Resposta inválida do servidor.",
        _ => Limitar(ex.InnerException?.Message ?? ex.Message, 500)
    };

    private static string Limitar(string texto, int max) =>
        texto.Length <= max ? texto : texto[..max];
}

