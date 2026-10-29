using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoPessoa = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NomeRazaoSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CodigoMunicipioIbge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clientes_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_EmpresaId_CpfCnpj",
                table: "clientes",
                columns: new[] { "EmpresaId", "CpfCnpj" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clientes_EmpresaId_NomeRazaoSocial",
                table: "clientes",
                columns: new[] { "EmpresaId", "NomeRazaoSocial" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
