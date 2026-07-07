using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CESICICD.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationAndAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "Listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "Listings");
        }
    }
}
