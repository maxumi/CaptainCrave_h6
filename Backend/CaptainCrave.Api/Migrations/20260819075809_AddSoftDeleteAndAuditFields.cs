using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptainCrave.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "restaurants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "restaurants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "restaurants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "order_items",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "order_items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "menus",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "menus",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "menu_items",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "menu_items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "categories",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "categories",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "categories");
        }
    }
}
