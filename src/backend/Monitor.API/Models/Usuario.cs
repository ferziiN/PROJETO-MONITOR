namespace Monitor.API.Models;

/// <summary>
/// Usuário da plataforma Monitor com perfil de acesso (Admin ou Usuario).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Identificador de login (ex.: "admin" ou "usuario").</summary>
    public string Login { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Usuario;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
