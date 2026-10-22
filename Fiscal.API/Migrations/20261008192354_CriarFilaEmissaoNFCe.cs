using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class CriarFilaEmissaoNFCe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitacoes_emissao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Modelo = table.Column<short>(type: "smallint", nullable: false),
                    Ambiente = table.Column<short>(type: "smallint", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Protocolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CStat = table.Column<int>(type: "integer", nullable: true),
                    XMotivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    XmlAutorizado = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Erro = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitacoes_emissao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_solicitacoes_emissao_agentes_fiscais_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "agentes_fiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_solicitacoes_emissao_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_emissao_AgenteId",
                table: "solicitacoes_emissao",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_emissao_EmpresaId_Ambiente_Modelo_Serie_Numero",
                table: "solicitacoes_emissao",
                columns: new[] { "EmpresaId", "Ambiente", "Modelo", "Serie", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_emissao_EmpresaId_Status_CriadoEm",
                table: "solicitacoes_emissao",
                columns: new[] { "EmpresaId", "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_emissao_NotaFiscalId",
                table: "solicitacoes_emissao",
                column: "NotaFiscalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitacoes_emissao");
        }
    }
}
