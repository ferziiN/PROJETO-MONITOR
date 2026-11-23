using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Monitor.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alertas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServicoId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StatusAnterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StatusNovo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reconhecido = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alertas_servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alertas_Reconhecido_CriadoEm",
                table: "alertas",
                columns: new[] { "Reconhecido", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_alertas_ServicoId_CriadoEm",
                table: "alertas",
                columns: new[] { "ServicoId", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas");
        }
    }
}
