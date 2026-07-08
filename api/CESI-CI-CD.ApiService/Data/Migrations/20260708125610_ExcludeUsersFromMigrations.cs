using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CESICICD.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExcludeUsersFromMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vide intentionnellement : ce marqueur de migration n'enregistre que le changement
            // de modèle "Users exclue des migrations d'ApiService" dans l'historique EF, il n'y a
            // pas d'opération SQL à exécuter (IdentityService possède désormais ce schéma).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vide intentionnellement, voir Up().
        }
    }
}
