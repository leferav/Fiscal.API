using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class CriarLogsFiscais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "logs_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Evento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Nivel = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Mensagem = table.Column<string>(type: "text", nullable: true),
                    DetalhesErro = table.Column<string>(type: "text", nullable: true),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Numero = table.Column<long>(type: "bigint", nullable: true),
                    Serie = table.Column<int>(type: "integer", nullable: true),
                    CStat = table.Column<int>(type: "integer", nullable: true),
                    Protocolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_logs_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_logs_fiscais_notas_fiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "notas_fiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_logs_fiscais_EmpresaId_CriadoEm",
                table: "logs_fiscais",
                columns: new[] { "EmpresaId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_logs_fiscais_NotaFiscalId",
                table: "logs_fiscais",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_logs_fiscais_VendaId",
                table: "logs_fiscais",
                column: "VendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logs_fiscais");
        }
    }
}
