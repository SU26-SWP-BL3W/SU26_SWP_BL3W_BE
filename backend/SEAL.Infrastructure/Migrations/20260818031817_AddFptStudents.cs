using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFptStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FptStudents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Major = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Campus = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnrollYear = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FptStudents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FptStudents_StudentCode",
                table: "FptStudents",
                column: "StudentCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FptStudents");
        }
    }
}
