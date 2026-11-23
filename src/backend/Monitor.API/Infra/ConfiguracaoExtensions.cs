using System.Net;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Monitor.API.Controllers;
using Monitor.API.Data;
using Monitor.API.Models;
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
                // Enums trafegam como texto: "Online", "Https", "Admin"...
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddHealthChecks().AddCheck<BancoDeDadosHealthCheck>("database");

        return services;
    }

    public static IServiceCollection AdicionarBancoDeDados(this IServiceCollection services, IConfiguration config)
    {
        var provedor = config["BancoDeDados:Provedor"] ?? "PostgreSQL";
        var connectionString = config.GetConnectionString("MonitorDb")
            ?? throw new InvalidOperationException(
                "Connection string 'MonitorDb' não configurada (appsettings ou variável ConnectionStrings__MonitorDb).");

        services.AddDbContext<MonitorDbContext>(o =>
        {
            if (string.Equals(provedor, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                o.UseSqlite(connectionString);
            }
            else
            {
                o.UseNpgsql(connectionString);
            }
        });

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
    /// Inicializa o esquema do banco ao iniciar (MigrateAsync para PostgreSQL, EnsureCreatedAsync para SQLite)
    /// e popula usuários padrão (admin / usuario) e serviços de demonstração.
    /// </summary>
    public static async Task AplicarMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("BancoDeDados:AplicarMigrationsAoIniciar")) return;

        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
            var provedor = app.Configuration["BancoDeDados:Provedor"] ?? "PostgreSQL";

            if (string.Equals(provedor, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureCreatedAsync();

                // Caso o arquivo SQLite seja de uma versão anterior do esquema (sem tabela usuarios), recria em dev.
                try
                {
                    _ = await db.Usuarios.AnyAsync();
                }
                catch
                {
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();
                }

                app.Logger.LogInformation("Banco local SQLite inicializado com sucesso.");
            }
            else
            {
                await db.Database.MigrateAsync();
                app.Logger.LogInformation("Migrations PostgreSQL aplicadas com sucesso.");
            }

            if (!await db.Usuarios.AnyAsync())
            {
                db.Usuarios.AddRange(
                    new Usuario
                    {
                        Nome = "Carlos Eduardo (Admin TI)",
                        Login = "admin",
                        SenhaHash = AuthController.GerarHashSimples("admin123"),
                        Perfil = PerfilUsuario.Admin,
                        Ativo = true
                    },
                    new Usuario
                    {
                        Nome = "Operador NOC (Usuário)",
                        Login = "usuario",
                        SenhaHash = AuthController.GerarHashSimples("usuario123"),
                        Perfil = PerfilUsuario.Usuario,
                        Ativo = true
                    });

                await db.SaveChangesAsync();
                app.Logger.LogInformation("Perfis padrão 'admin' e 'usuario' criados.");
            }

            if (app.Configuration.GetValue<bool>("BancoDeDados:PopularDadosIniciais")
                && !await db.Servicos.AnyAsync())
            {
                var agora = DateTime.UtcNow;
                db.Servicos.AddRange(
                    new Servico
                    {
                        Nome = "Monitor API (Self Health)",
                        Endereco = "localhost",
                        Porta = 5094,
                        Endpoint = "/health",
                        Tipo = TipoProtocolo.Http,
                        IntervaloSegundos = 30,
                        Ativo = true,
                        ProximaVerificacaoEm = agora
                    },
                    new Servico
                    {
                        Nome = "Google DNS Web",
                        Endereco = "dns.google",
                        Porta = 443,
                        Endpoint = "/",
                        Tipo = TipoProtocolo.Https,
                        IntervaloSegundos = 60,
                        Ativo = true,
                        ProximaVerificacaoEm = agora
                    },
                    new Servico
                    {
                        Nome = "Servidor Legado (Simulação Offline)",
                        Endereco = "localhost",
                        Porta = 59999,
                        Endpoint = "/status",
                        Tipo = TipoProtocolo.Http,
                        IntervaloSegundos = 45,
                        Ativo = true,
                        ProximaVerificacaoEm = agora
                    });

                await db.SaveChangesAsync();
                app.Logger.LogInformation("Serviços iniciais de demonstração cadastrados.");
            }
        }
        catch (Exception ex)
        {
            // Não derruba a API: o /health vai indicar o problema com o banco.
            app.Logger.LogError(ex, "Não foi possível inicializar o banco de dados.");
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
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = arquivos,
            OnPrepareResponse = ctx =>
            {
                // Evita cache de arquivos JS/CSS/HTML no navegador durante o desenvolvimento
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                ctx.Context.Response.Headers.Pragma = "no-cache";
                ctx.Context.Response.Headers.Expires = "0";
            }
        });

        app.Logger.LogInformation("Front-end servido a partir de {Caminho}", caminho);
    }
}
