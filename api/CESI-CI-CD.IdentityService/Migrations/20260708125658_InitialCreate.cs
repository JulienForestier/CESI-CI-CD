using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CESICICD.IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL idempotent plutôt que CreateTable/CreateIndex générés : sur dev/rec/prod, la
            // table "Users" existe déjà (créée historiquement par les migrations de
            // CESI-CI-CD.ApiService, avant que l'IdentityService n'existe) avec exactement ce
            // schéma. Cette migration doit fonctionner à la fois sur ces bases existantes (no-op)
            // et sur une base neuve (création réelle) — IdentityService devient le propriétaire
            // des migrations de cette table à partir de maintenant.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id" uuid NOT NULL,
                    "Email" text NOT NULL,
                    "PasswordHash" text NOT NULL,
                    "DisplayName" text NOT NULL,
                    "IsAdmin" boolean NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
                )
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email")
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "Users" """);
        }
    }
}
