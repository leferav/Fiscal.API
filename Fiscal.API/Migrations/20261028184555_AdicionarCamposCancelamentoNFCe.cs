using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiscal.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposCancelamentoNFCe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CStatCancelamento",
                table: "notas_fiscais",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceladoEm",
                table: "notas_fiscais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelamentoSolicitadoEm",
                table: "notas_fiscais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativaCancelamento",
                table: "notas_fiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelamento",
                table: "notas_fiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocoloCancelamento",
                table: "notas_fiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XmlCancelamentoEnvio",
                table: "notas_fiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XmlCancelamentoRetorno",
                table: "notas_fiscais",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CStatCancelamento",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "CanceladoEm",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "CancelamentoSolicitadoEm",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "JustificativaCancelamento",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "MotivoCancelamento",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "ProtocoloCancelamento",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "XmlCancelamentoEnvio",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "XmlCancelamentoRetorno",
                table: "notas_fiscais");
        }
    }
}
