using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashKorea.Migrations
{
    /// <inheritdoc />
    public partial class Add_TourMapReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "TourMaps",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "TourMapReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TourMapId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rating = table.Column<double>(type: "double", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourMapReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourMapReviews_TourMaps_TourMapId",
                        column: x => x.TourMapId,
                        principalTable: "TourMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourMapReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TourMapComments_TourMapId",
                table: "TourMapComments",
                column: "TourMapId");

            migrationBuilder.CreateIndex(
                name: "IX_TourMapReviews_TourMapId",
                table: "TourMapReviews",
                column: "TourMapId");

            migrationBuilder.CreateIndex(
                name: "IX_TourMapReviews_UserId",
                table: "TourMapReviews",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TourMapComments_TourMaps_TourMapId",
                table: "TourMapComments",
                column: "TourMapId",
                principalTable: "TourMaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TourMapComments_TourMaps_TourMapId",
                table: "TourMapComments");

            migrationBuilder.DropTable(
                name: "TourMapReviews");

            migrationBuilder.DropIndex(
                name: "IX_TourMapComments_TourMapId",
                table: "TourMapComments");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "TourMaps");
        }
    }
}
