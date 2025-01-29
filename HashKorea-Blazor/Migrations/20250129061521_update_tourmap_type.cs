using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashKorea.Migrations
{
    /// <inheritdoc />
    public partial class update_tourmap_type : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "TourMaps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CategoryCD",
                table: "TourMaps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MainImagePublicUrl",
                table: "TourMaps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MainImageStoragePath",
                table: "TourMaps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "TourMaps");

            migrationBuilder.DropColumn(
                name: "CategoryCD",
                table: "TourMaps");

            migrationBuilder.DropColumn(
                name: "MainImagePublicUrl",
                table: "TourMaps");

            migrationBuilder.DropColumn(
                name: "MainImageStoragePath",
                table: "TourMaps");
        }
    }
}
