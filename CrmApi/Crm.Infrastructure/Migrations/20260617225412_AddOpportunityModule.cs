using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByPersonelId",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Opportunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CreatedByPersonelId",
                table: "Opportunities",
                column: "CreatedByPersonelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Opportunities_Personels_CreatedByPersonelId",
                table: "Opportunities",
                column: "CreatedByPersonelId",
                principalTable: "Personels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Opportunities_Personels_CreatedByPersonelId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_CreatedByPersonelId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "CreatedByPersonelId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Opportunities");
        }
    }
}
