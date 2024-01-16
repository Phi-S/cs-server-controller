using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedSystemLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerLogs_ServerStarts_ServerStartId",
                table: "ServerLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UpdateOrInstallLogs_UpdateOrInstallStarts_UpdateOrInstallStartId",
                table: "UpdateOrInstallLogs");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                table: "UpdateOrInstallStarts",
                newName: "StartedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "UpdateOrInstallStarts",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "UpdateOrInstallStartId",
                table: "UpdateOrInstallLogs",
                newName: "UpdateOrInstallStartDbModelId");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "UpdateOrInstallLogs",
                newName: "CreatedUtc");

            migrationBuilder.RenameIndex(
                name: "IX_UpdateOrInstallLogs_UpdateOrInstallStartId",
                table: "UpdateOrInstallLogs",
                newName: "IX_UpdateOrInstallLogs_UpdateOrInstallStartDbModelId");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                table: "ServerStarts",
                newName: "StartedUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ServerStarts",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "ServerStartId",
                table: "ServerLogs",
                newName: "ServerStartDbModelId");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ServerLogs",
                newName: "CreatedUtc");

            migrationBuilder.RenameIndex(
                name: "IX_ServerLogs_ServerStartId",
                table: "ServerLogs",
                newName: "IX_ServerLogs_ServerStartDbModelId");

            migrationBuilder.RenameColumn(
                name: "TriggeredAtUtc",
                table: "EvenLogs",
                newName: "TriggeredUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "EvenLogs",
                newName: "CreatedUtc");

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ServerLogs_ServerStarts_ServerStartDbModelId",
                table: "ServerLogs",
                column: "ServerStartDbModelId",
                principalTable: "ServerStarts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateOrInstallLogs_UpdateOrInstallStarts_UpdateOrInstallStartDbModelId",
                table: "UpdateOrInstallLogs",
                column: "UpdateOrInstallStartDbModelId",
                principalTable: "UpdateOrInstallStarts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerLogs_ServerStarts_ServerStartDbModelId",
                table: "ServerLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UpdateOrInstallLogs_UpdateOrInstallStarts_UpdateOrInstallStartDbModelId",
                table: "UpdateOrInstallLogs");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.RenameColumn(
                name: "StartedUtc",
                table: "UpdateOrInstallStarts",
                newName: "StartedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "UpdateOrInstallStarts",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdateOrInstallStartDbModelId",
                table: "UpdateOrInstallLogs",
                newName: "UpdateOrInstallStartId");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "UpdateOrInstallLogs",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_UpdateOrInstallLogs_UpdateOrInstallStartDbModelId",
                table: "UpdateOrInstallLogs",
                newName: "IX_UpdateOrInstallLogs_UpdateOrInstallStartId");

            migrationBuilder.RenameColumn(
                name: "StartedUtc",
                table: "ServerStarts",
                newName: "StartedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "ServerStarts",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ServerStartDbModelId",
                table: "ServerLogs",
                newName: "ServerStartId");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "ServerLogs",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_ServerLogs_ServerStartDbModelId",
                table: "ServerLogs",
                newName: "IX_ServerLogs_ServerStartId");

            migrationBuilder.RenameColumn(
                name: "TriggeredUtc",
                table: "EvenLogs",
                newName: "TriggeredAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "EvenLogs",
                newName: "CreatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_ServerLogs_ServerStarts_ServerStartId",
                table: "ServerLogs",
                column: "ServerStartId",
                principalTable: "ServerStarts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateOrInstallLogs_UpdateOrInstallStarts_UpdateOrInstallStartId",
                table: "UpdateOrInstallLogs",
                column: "UpdateOrInstallStartId",
                principalTable: "UpdateOrInstallStarts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
