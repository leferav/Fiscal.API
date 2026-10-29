using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarClienteIdVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "vendas",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "vendas");
        }
    }
}
