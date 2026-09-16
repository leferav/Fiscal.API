using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    InscricaoEstadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ambiente = table.Column<short>(type: "smallint", nullable: false),
                    SerieNFe = table.Column<int>(type: "integer", nullable: false),
                    SerieNFCe = table.Column<int>(type: "integer", nullable: false),
                    ProximoNumeroNFe = table.Column<long>(type: "bigint", nullable: false),
                    ProximoNumeroNFCe = table.Column<long>(type: "bigint", nullable: false),
                    Csc = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CscId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracoes_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Modelo = table.Column<short>(type: "smallint", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Ambiente = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Recibo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CStat = table.Column<int>(type: "integer", nullable: true),
                    XMotivo = table.Column<string>(type: "text", nullable: true),
                    ValorProdutos = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true),
                    XmlAutorizado = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutorizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notas_fiscais_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracoes_fiscais_EmpresaId",
                table: "configuracoes_fiscais",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_empresas_Cnpj",
                table: "empresas",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_ChaveAcesso",
                table: "notas_fiscais",
                column: "ChaveAcesso");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_EmpresaId_Ambiente_Modelo_Serie_Numero",
                table: "notas_fiscais",
                columns: new[] { "EmpresaId", "Ambiente", "Modelo", "Serie", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_Status",
                table: "notas_fiscais",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes_fiscais");

            migrationBuilder.DropTable(
                name: "notas_fiscais");

            migrationBuilder.DropTable(
                name: "empresas");
        }
    }
}
