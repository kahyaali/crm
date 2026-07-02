using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByPersonelId",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatedByPersonelId",
                table: "Campaigns",
                column: "CreatedByPersonelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Personels_CreatedByPersonelId",
                table: "Campaigns",
                column: "CreatedByPersonelId",
                principalTable: "Personels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Personels_CreatedByPersonelId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_CreatedByPersonelId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "CreatedByPersonelId",
                table: "Campaigns");
        }
    }
}
