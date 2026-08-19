using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptainCrave.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToMenusAndMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "menus",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "menus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "menu_items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "menu_items",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "menu_items");
        }
    }
}
