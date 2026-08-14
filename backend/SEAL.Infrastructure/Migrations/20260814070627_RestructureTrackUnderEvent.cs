using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestructureTrackUnderEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Rounds_RoundId",
                table: "Tracks");

            migrationBuilder.AlterColumn<string>(
                name: "RoundId",
                table: "Tracks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "EventId",
                table: "Tracks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrackId",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoundId",
                table: "SubmitResults",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_EventId",
                table: "Tracks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TrackId",
                table: "Teams",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmitResults_RoundId",
                table: "SubmitResults",
                column: "RoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmitResults_Rounds_RoundId",
                table: "SubmitResults",
                column: "RoundId",
                principalTable: "Rounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Tracks_TrackId",
                table: "Teams",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Events_EventId",
                table: "Tracks",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Rounds_RoundId",
                table: "Tracks",
                column: "RoundId",
                principalTable: "Rounds",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmitResults_Rounds_RoundId",
                table: "SubmitResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Tracks_TrackId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Events_EventId",
                table: "Tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Rounds_RoundId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_EventId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TrackId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_SubmitResults_RoundId",
                table: "SubmitResults");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RoundId",
                table: "SubmitResults");

            migrationBuilder.AlterColumn<string>(
                name: "RoundId",
                table: "Tracks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Rounds_RoundId",
                table: "Tracks",
                column: "RoundId",
                principalTable: "Rounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
