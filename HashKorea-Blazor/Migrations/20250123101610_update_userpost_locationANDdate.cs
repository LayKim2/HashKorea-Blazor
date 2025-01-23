using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashKorea.Migrations
{
    /// <inheritdoc />
    public partial class update_userpost_locationANDdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "UserPosts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "UserPosts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationCD",
                table: "UserPosts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "UserPosts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "UserPosts");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "UserPosts");

            migrationBuilder.DropColumn(
                name: "LocationCD",
                table: "UserPosts");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "UserPosts");
        }
    }
}
