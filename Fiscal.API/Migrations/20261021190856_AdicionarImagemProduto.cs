using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarImagemProduto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "produtos_imagens",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    ProdutoId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    Imagem = table.Column<byte[]>(
                        type: "bytea",
                        nullable: false),

                    ContentType = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false),

                    TamanhoBytes = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    CriadoEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_produtos_imagens",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_produtos_imagens_produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_produtos_imagens_ProdutoId",
                table: "produtos_imagens",
                column: "ProdutoId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "produtos_imagens");
        }

    }
}
