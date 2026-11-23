using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monitor.API.Models;

namespace Monitor.API.Data.Configurations;

public class AlertaConfiguration : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("alertas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.StatusAnterior).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.StatusNovo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Mensagem).HasMaxLength(500).IsRequired();
        builder.Property(a => a.CriadoEm).IsRequired();

        builder.HasOne(a => a.Servico)
            .WithMany()
            .HasForeignKey(a => a.ServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.Reconhecido, a.CriadoEm });
        builder.HasIndex(a => new { a.ServicoId, a.CriadoEm });
    }
}
