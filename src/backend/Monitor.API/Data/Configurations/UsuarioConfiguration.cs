using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monitor.API.Models;

namespace Monitor.API.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Login).HasMaxLength(60).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.Perfil).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.Ativo).HasDefaultValue(true);

        builder.HasIndex(u => u.Login).IsUnique();
    }
}
