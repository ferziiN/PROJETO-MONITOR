// Ponto de entrada da API do Monitor (ASP.NET Core).
// Os registros de dependências ficam em Infra/ConfiguracaoExtensions.cs.
using Monitor.API.Infra;
using Monitor.API.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Serviços (injeção de dependência)
// ---------------------------------------------------------------------------
builder.Services
    .AdicionarApi()
    .AdicionarBancoDeDados(builder.Configuration)
    .AdicionarMonitoramento(builder.Configuration)
    .AdicionarCors(builder.Configuration);

builder.Services.AddScoped<IServicoService, ServicoService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline HTTP
// ---------------------------------------------------------------------------
app.UseExceptionHandler();   // erros não tratados viram ProblemDetails (JSON)
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // /openapi/v1.json
    app.MapScalarApiReference();   // /scalar — interface para testar os endpoints
    app.UsarFrontendEstatico();    // serve src/frontend em http://localhost:5094/
}

// Obs.: sem UseHttpsRedirection — em produção o TLS é encerrado no Nginx (proxy reverso).

app.UseCors(ConfiguracaoExtensions.PoliticaCors);

app.MapControllers();
app.MapHealthChecks("/health");

await app.AplicarMigrationsAsync();

app.Run();
