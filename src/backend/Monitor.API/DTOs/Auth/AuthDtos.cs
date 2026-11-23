using System.ComponentModel.DataAnnotations;
using Monitor.API.Models;

namespace Monitor.API.DTOs.Auth;

public record LoginRequest
{
    [Required(ErrorMessage = "Informe o usuário.")]
    public string Login { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Senha { get; init; } = string.Empty;
}

public record LoginResponse(
    int Id,
    string Nome,
    string Login,
    PerfilUsuario Perfil,
    string Token,
    DateTime AutenticadoEm,
    IReadOnlyList<string> TelasAutorizadas);
