using System.Net;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Monitor.API.Data;
using Monitor.API.Services.Monitoramento;

namespace Monitor.API.Infra;

/// <summary>
/// Registro de dependências e configuração do pipeline, para manter o Program.cs enxuto.
/// </summary>
public static class ConfiguracaoExtensions
{
    public const string PoliticaCors = "Frontend";

    public static IServiceCollection AdicionarApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(o =>
            {
                // Enums trafegam como texto: "Online", "Https"...
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddHealthChecks().AddCheck<BancoDeDadosHealthCheck>("postgresql");

        return services;
    }

    public static IServiceCollection AdicionarBancoDeDados(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("MonitorDb")
            ?? throw new InvalidOperationException(
                "Connection string 'MonitorDb' não configurada (appsettings ou variável ConnectionStrings__MonitorDb).");

        services.AddDbContext<MonitorDbContext>(o => o.UseNpgsql(connectionString));
        return services;
    }

    public static IServiceCollection AdicionarMonitoramento(this IServiceCollection services, IConfiguration config)
    {
        var secao = config.GetSection(MonitoramentoOptions.Secao);
        services.Configure<MonitoramentoOptions>(secao);
        var opcoes = secao.Get<MonitoramentoOptions>() ?? new MonitoramentoOptions();

        // IHttpClientFactory gerencia o pool de conexões e evita "socket exhaustion".
        services.AddHttpClient(VerificadorHttp.NomeHttpClient, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos + 5); // o timeout real é por requisição
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Monitor/1.0 (+health-check)");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos)
            });

        services.AddSingleton<IVerificadorHttp, VerificadorHttp>();
        services.AddScoped<IMonitoramentoService, MonitoramentoService>();
        services.AddHostedService<MonitoramentoWorker>();

        return services;
    }

    public static IServiceCollection AdicionarCors(this IServiceCollection services, IConfiguration config)
    {
        var origens = config.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];

        services.AddCors(o => o.AddPolicy(PoliticaCors, policy =>
        {
            // Sem origens configuradas = libera qualquer origem (apenas para desenvolvimento).
            if (origens.Length == 0) policy.AllowAnyOrigin();
            else policy.WithOrigins(origens);

            policy.AllowAnyHeader().AllowAnyMethod();
        }));

        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes ao iniciar (controlado por BancoDeDados:AplicarMigrationsAoIniciar).
    /// </summary>
    public static async Task AplicarMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("BancoDeDados:AplicarMigrationsAoIniciar")) return;

        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Migrations aplicadas com sucesso.");
        }
        catch (Exception ex)
        {
            // Não derruba a API: o /health vai indicar o problema com o banco.
            app.Logger.LogError(ex, "Não foi possível aplicar as migrations. Verifique se o PostgreSQL está no ar.");
        }
    }

    /// <summary>
    /// Em desenvolvimento, serve o front-end (src/frontend) pela própria API,
    /// evitando problemas de CORS e a necessidade de outro servidor.
    /// Em produção quem serve os arquivos estáticos é o Nginx.
    /// </summary>
    public static void UsarFrontendEstatico(this WebApplication app)
    {
        var caminhoConfigurado = app.Configuration["Frontend:Caminho"];
        if (string.IsNullOrWhiteSpace(caminhoConfigurado)) return;

        var caminho = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, caminhoConfigurado));
        if (!Directory.Exists(caminho))
        {
            app.Logger.LogWarning("Pasta do front-end não encontrada: {Caminho}", caminho);
            return;
        }

        var arquivos = new PhysicalFileProvider(caminho);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = arquivos });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = arquivos });

        app.Logger.LogInformation("Front-end servido a partir de {Caminho}", caminho);
    }
}

