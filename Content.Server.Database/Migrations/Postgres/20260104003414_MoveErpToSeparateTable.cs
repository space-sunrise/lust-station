using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MoveErpToSeparateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the new table first
            migrationBuilder.CreateTable(
                name: "profile_erp",
                columns: table => new
                {
                    profile_erp_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    erp = table.Column<string>(type: "text", nullable: false),
                    virginity = table.Column<string>(type: "text", nullable: false),
                    anal_virginity = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_erp", x => x.profile_erp_id);
                    table.ForeignKey(
                        name: "FK_profile_erp_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_erp_profile_id",
                table: "profile_erp",
                column: "profile_id",
                unique: true);

            // Migrate data from profile to profile_erp
            migrationBuilder.Sql(@"
                INSERT INTO profile_erp (profile_id, erp, virginity, anal_virginity)
                SELECT profile_id, 
                       COALESCE(erp, 'Ask') as erp,
                       COALESCE(virginity, 'No') as virginity,
                       COALESCE(anal_virginity, 'Yes') as anal_virginity
                FROM profile");

            // Drop columns from profile
            migrationBuilder.DropColumn(
                name: "anal_virginity",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "erp",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "virginity",
                table: "profile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add columns back to profile
            migrationBuilder.AddColumn<string>(
                name: "anal_virginity",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "erp",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "virginity",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Migrate data back from profile_erp to profile
            migrationBuilder.Sql(@"
                UPDATE profile
                SET erp = (SELECT erp FROM profile_erp WHERE profile_erp.profile_id = profile.profile_id),
                    virginity = (SELECT virginity FROM profile_erp WHERE profile_erp.profile_id = profile.profile_id),
                    anal_virginity = (SELECT anal_virginity FROM profile_erp WHERE profile_erp.profile_id = profile.profile_id)
                WHERE EXISTS (SELECT 1 FROM profile_erp WHERE profile_erp.profile_id = profile.profile_id)");

            // Drop the profile_erp table
            migrationBuilder.DropTable(
                name: "profile_erp");
        }
    }
}
