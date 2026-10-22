using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAgenteFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agentes_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IdentificadorMaquina = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaComunicacaoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agentes_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agentes_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_fiscais_EmpresaId",
                table: "agentes_fiscais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_agentes_fiscais_EmpresaId_IdentificadorMaquina",
                table: "agentes_fiscais",
                columns: new[] { "EmpresaId", "IdentificadorMaquina" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agentes_fiscais");
        }
    }
}
