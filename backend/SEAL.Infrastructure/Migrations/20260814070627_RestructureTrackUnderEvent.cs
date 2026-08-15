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

            // Ba cot moi mac dinh chuoi rong -> vi pham khoa ngoai ben duoi neu bang
            // da co du lieu. Dien gia tri suy tu lien ket cu TRUOC khi rang buoc FK.
            // Voi DB trong (moi tao) cac lenh nay khong dong nao bi anh huong.

            // Bai nop: lay vong tu hang muc cu (Track.RoundId), lam truoc khi mat lien ket
            migrationBuilder.Sql(@"
                UPDATE ""SubmitResults"" sr
                SET ""RoundId"" = t.""RoundId""
                FROM ""Tracks"" t
                WHERE sr.""TrackId"" = t.""Id"" AND t.""RoundId"" IS NOT NULL;");

            // Hang muc: suy su kien qua vong
            migrationBuilder.Sql(@"
                UPDATE ""Tracks"" t
                SET ""EventId"" = r.""EventId""
                FROM ""Rounds"" r
                WHERE t.""RoundId"" = r.""Id"";");

            // Doi: suy hang muc tu bai nop som nhat, thieu thi lay tu phan cong EventRole
            migrationBuilder.Sql(@"
                UPDATE ""Teams"" tm
                SET ""TrackId"" = sub.""TrackId""
                FROM (
                    SELECT DISTINCT ON (""TeamId"") ""TeamId"", ""TrackId""
                    FROM ""SubmitResults"" WHERE ""IsActive"" = true
                    ORDER BY ""TeamId"", ""CreatedTime""
                ) sub
                WHERE tm.""Id"" = sub.""TeamId"";

                UPDATE ""Teams"" tm
                SET ""TrackId"" = er.""TrackId""
                FROM ""EventRoles"" er
                WHERE tm.""TrackId"" IS NULL AND er.""TeamId"" = tm.""Id"" AND er.""TrackId"" IS NOT NULL;");

            // Con dong nao chua suy duoc thi dung ngay, dung de FK bao loi kho hieu
            migrationBuilder.Sql(@"
                DO $$
                DECLARE bad_tracks int; bad_subs int;
                BEGIN
                    SELECT count(*) INTO bad_tracks FROM ""Tracks"" WHERE ""EventId"" = '';
                    SELECT count(*) INTO bad_subs   FROM ""SubmitResults"" WHERE ""RoundId"" = '';
                    IF bad_tracks > 0 OR bad_subs > 0 THEN
                        RAISE EXCEPTION 'Khong suy duoc du lieu: % hang muc thieu EventId, % bai nop thieu RoundId', bad_tracks, bad_subs;
                    END IF;
                END $$;");

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
