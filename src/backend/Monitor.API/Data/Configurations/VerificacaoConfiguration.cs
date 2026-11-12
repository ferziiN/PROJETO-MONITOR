using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monitor.API.Models;

namespace Monitor.API.Data.Configurations;

public class VerificacaoConfiguration : IEntityTypeConfiguration<Verificacao>
{
    public void Configure(EntityTypeBuilder<Verificacao> builder)
    {
        builder.ToTable("verificacoes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.MensagemErro).HasMaxLength(500);
        builder.Property(v => v.VerificadoEm).IsRequired();

        // Consultas de histórico são sempre "por serviço, mais recentes primeiro".
        builder.HasIndex(v => new { v.ServicoId, v.VerificadoEm });

        // Usado pela rotina de retenção (limpeza de registros antigos).
        builder.HasIndex(v => v.VerificadoEm);
    }
}

