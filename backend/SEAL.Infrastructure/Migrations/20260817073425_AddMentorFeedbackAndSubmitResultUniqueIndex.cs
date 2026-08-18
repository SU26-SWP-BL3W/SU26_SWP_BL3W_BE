using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorFeedbackAndSubmitResultUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmitResults_TeamId",
                table: "SubmitResults");

            migrationBuilder.CreateTable(
                name: "MentorFeedbacks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SubmitResultId = table.Column<string>(type: "text", nullable: false),
                    MentorId = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MentorFeedbacks_SubmitResults_SubmitResultId",
                        column: x => x.SubmitResultId,
                        principalTable: "SubmitResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MentorFeedbacks_Users_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmitResults_TeamId_TrackId_RoundId",
                table: "SubmitResults",
                columns: new[] { "TeamId", "TrackId", "RoundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MentorFeedbacks_MentorId",
                table: "MentorFeedbacks",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_MentorFeedbacks_SubmitResultId",
                table: "MentorFeedbacks",
                column: "SubmitResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MentorFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_SubmitResults_TeamId_TrackId_RoundId",
                table: "SubmitResults");

            migrationBuilder.CreateIndex(
                name: "IX_SubmitResults_TeamId",
                table: "SubmitResults",
                column: "TeamId");
        }
    }
}
