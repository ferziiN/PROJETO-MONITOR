using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monitor.API.Models;

namespace Monitor.API.Data.Configurations;

public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("servicos");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nome).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Endereco).HasMaxLength(255).IsRequired();
        builder.Property(s => s.Endpoint).HasMaxLength(500).IsRequired().HasDefaultValue("/");

        // Enums gravados como texto para facilitar a leitura direto no banco.
        builder.Property(s => s.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(s => s.StatusAtual).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(s => s.IntervaloSegundos).IsRequired();
        builder.Property(s => s.Ativo).HasDefaultValue(true);

        builder.HasMany(s => s.Verificacoes)
            .WithOne(v => v.Servico)
            .HasForeignKey(v => v.ServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Usado pelo agendador para buscar os serviços "vencidos".
        builder.HasIndex(s => new { s.Ativo, s.ProximaVerificacaoEm });
    }
}

