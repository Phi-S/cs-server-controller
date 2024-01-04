using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLib.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvenLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerStarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartParameters = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerStarts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateOrInstallStarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateOrInstallStarts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerStartId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerLogs_ServerStarts_ServerStartId",
                        column: x => x.ServerStartId,
                        principalTable: "ServerStarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpdateOrInstallLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UpdateOrInstallStartId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateOrInstallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpdateOrInstallLogs_UpdateOrInstallStarts_UpdateOrInstallStartId",
                        column: x => x.UpdateOrInstallStartId,
                        principalTable: "UpdateOrInstallStarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerLogs_ServerStartId",
                table: "ServerLogs",
                column: "ServerStartId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateOrInstallLogs_UpdateOrInstallStartId",
                table: "UpdateOrInstallLogs",
                column: "UpdateOrInstallStartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvenLogs");

            migrationBuilder.DropTable(
                name: "ServerLogs");

            migrationBuilder.DropTable(
                name: "UpdateOrInstallLogs");

            migrationBuilder.DropTable(
                name: "ServerStarts");

            migrationBuilder.DropTable(
                name: "UpdateOrInstallStarts");
        }
    }
}
