using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Monitor.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Porta = table.Column<int>(type: "integer", nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "/"),
                    Tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IntervaloSegundos = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusAtual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UltimoTempoRespostaMs = table.Column<int>(type: "integer", nullable: true),
                    UltimaVerificacaoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProximaVerificacaoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verificacoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServicoId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TempoRespostaMs = table.Column<int>(type: "integer", nullable: true),
                    CodigoHttp = table.Column<int>(type: "integer", nullable: true),
                    VerificadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MensagemErro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_verificacoes_servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_servicos_Ativo_ProximaVerificacaoEm",
                table: "servicos",
                columns: new[] { "Ativo", "ProximaVerificacaoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_verificacoes_ServicoId_VerificadoEm",
                table: "verificacoes",
                columns: new[] { "ServicoId", "VerificadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_verificacoes_VerificadoEm",
                table: "verificacoes",
                column: "VerificadoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "verificacoes");

            migrationBuilder.DropTable(
                name: "servicos");
        }
    }
}
