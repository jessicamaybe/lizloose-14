using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class DripTrack_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "drip_track",
                columns: table => new
                {
                    drip_track_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    drip_name = table.Column<string>(type: "TEXT", nullable: false),
                    rounds_left = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drip_track", x => x.drip_track_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_drip_track_player_id_drip_name",
                table: "drip_track",
                columns: new[] { "player_id", "drip_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "drip_track");
        }
    }
}
