using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContractEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByPersonelId",
                table: "Contracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSigned",
                table: "Contracts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuoteId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedBy",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedDate",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CreatedByPersonelId",
                table: "Contracts",
                column: "CreatedByPersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts",
                column: "QuoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Personels_CreatedByPersonelId",
                table: "Contracts",
                column: "CreatedByPersonelId",
                principalTable: "Personels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Quotes_QuoteId",
                table: "Contracts",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Personels_CreatedByPersonelId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Quotes_QuoteId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_CreatedByPersonelId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CreatedByPersonelId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "IsSigned",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "QuoteId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignedBy",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignedDate",
                table: "Contracts");
        }
    }
}
