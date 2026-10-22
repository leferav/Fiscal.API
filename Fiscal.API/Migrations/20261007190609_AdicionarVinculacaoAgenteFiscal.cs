using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVinculacaoAgenteFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vinculacoes_agentes_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Utilizado = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UtilizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculacoes_agentes_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vinculacoes_agentes_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vinculacoes_agentes_fiscais_CodigoHash",
                table: "vinculacoes_agentes_fiscais",
                column: "CodigoHash");

            migrationBuilder.CreateIndex(
                name: "IX_vinculacoes_agentes_fiscais_EmpresaId",
                table: "vinculacoes_agentes_fiscais",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_vinculacoes_agentes_fiscais_ExpiraEm",
                table: "vinculacoes_agentes_fiscais",
                column: "ExpiraEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vinculacoes_agentes_fiscais");
        }
    }
}
