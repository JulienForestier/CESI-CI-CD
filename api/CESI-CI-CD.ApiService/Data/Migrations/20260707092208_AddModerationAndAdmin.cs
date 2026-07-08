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
            // SQL idempotent : IdentityService inclut déjà "IsAdmin" dans sa propre création
            // (idempotente) de "Users" — si son InitialCreate s'exécute avant celui-ci, la
            // colonne existe déjà et AddColumn générerait un "column already exists".
            migrationBuilder.Sql("""
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsAdmin" boolean NOT NULL DEFAULT FALSE
                """);

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
            // "IsAdmin" n'est pas supprimée ici : la colonne est partagée avec le schéma que
            // CESI-CI-CD.IdentityService possède désormais pour la table "Users".

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "Listings");
        }
    }
}
