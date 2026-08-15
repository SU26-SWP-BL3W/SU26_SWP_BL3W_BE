using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionUrlsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubmissionUrl",
                table: "SubmitResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "DemoUrl",
                table: "SubmitResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepoFullName",
                table: "SubmitResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepoHost",
                table: "SubmitResults",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepoLastPush",
                table: "SubmitResults",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepoStars",
                table: "SubmitResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepoUrl",
                table: "SubmitResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlideUrl",
                table: "SubmitResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_UserId_IsRead_CreatedTime",
                table: "AppNotifications",
                columns: new[] { "UserId", "IsRead", "CreatedTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "DemoUrl",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "RepoFullName",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "RepoHost",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "RepoLastPush",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "RepoStars",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "RepoUrl",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "SlideUrl",
                table: "SubmitResults");

            migrationBuilder.AlterColumn<string>(
                name: "SubmissionUrl",
                table: "SubmitResults",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
        }
    }
}
