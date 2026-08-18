using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackIdToPrize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackId",
                table: "Prizes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prizes_TrackId",
                table: "Prizes",
                column: "TrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prizes_Tracks_TrackId",
                table: "Prizes",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prizes_Tracks_TrackId",
                table: "Prizes");

            migrationBuilder.DropIndex(
                name: "IX_Prizes_TrackId",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "Prizes");
        }
    }
}
