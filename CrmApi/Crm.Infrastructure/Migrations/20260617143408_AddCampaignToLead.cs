using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CampaignId",
                table: "Leads",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Campaigns_CampaignId",
                table: "Leads",
                column: "CampaignId",
                principalTable: "Campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Campaigns_CampaignId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_CampaignId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Leads");
        }
    }
}
