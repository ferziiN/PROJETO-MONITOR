using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitor.API.Data;
using Monitor.API.DTOs.Auth;
using Monitor.API.Models;

namespace Monitor.API.Controllers;

/// <summary>
/// Autenticação de usuários e definição de perfis (ADMIN vs USUÁRIO).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(MonitorDbContext db) : ControllerBase
{
    /// <summary>Autentica um usuário e retorna seu perfil (Admin ou Usuario) e permissões.</summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var loginNormalizado = request.Login.Trim().ToLowerInvariant();
        var hash = GerarHashSimples(request.Senha);

        var usuario = await db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Ativo && u.Login == loginNormalizado, ct);

        if (usuario is null || usuario.SenhaHash != hash)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciais inválidas",
                detail: "Usuário ou senha incorretos.");
        }

        // Admin possui acesso irrestrito ("*"); Usuário possui lista configurável de telas.
        IReadOnlyList<string> telasAutorizadas = usuario.Perfil == PerfilUsuario.Admin
            ? ["*"]
            : ["dashboard", "servicos", "alertas", "servico-detalhe"];

        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{usuario.Id}:{usuario.Login}:{usuario.Perfil}:{DateTime.UtcNow:O}"));

        return Ok(new LoginResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Login,
            usuario.Perfil,
            token,
            DateTime.UtcNow,
            telasAutorizadas));
    }

    public static string GerarHashSimples(string senha)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"MonitorSalt::{senha}"));
        return Convert.ToHexString(bytes);
    }
}
