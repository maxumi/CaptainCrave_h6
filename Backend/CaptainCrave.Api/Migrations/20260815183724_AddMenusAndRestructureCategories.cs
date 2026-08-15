using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptainCrave.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMenusAndRestructureCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_restaurants_restaurant_id",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_menu_items_restaurants_restaurant_id",
                table: "menu_items");

            migrationBuilder.RenameColumn(
                name: "restaurant_id",
                table: "menu_items",
                newName: "menu_id");

            migrationBuilder.RenameIndex(
                name: "IX_menu_items_restaurant_id",
                table: "menu_items",
                newName: "IX_menu_items_menu_id");

            migrationBuilder.RenameColumn(
                name: "restaurant_id",
                table: "categories",
                newName: "menu_id");

            migrationBuilder.RenameIndex(
                name: "IX_categories_restaurant_id",
                table: "categories",
                newName: "IX_categories_menu_id");

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    restaurant_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menus", x => x.id);
                    table.ForeignKey(
                        name: "FK_menus_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_menus_restaurant_id",
                table: "menus",
                column: "restaurant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_menus_menu_id",
                table: "categories",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_menu_items_menus_menu_id",
                table: "menu_items",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_menus_menu_id",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_menu_items_menus_menu_id",
                table: "menu_items");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.RenameColumn(
                name: "menu_id",
                table: "menu_items",
                newName: "restaurant_id");

            migrationBuilder.RenameIndex(
                name: "IX_menu_items_menu_id",
                table: "menu_items",
                newName: "IX_menu_items_restaurant_id");

            migrationBuilder.RenameColumn(
                name: "menu_id",
                table: "categories",
                newName: "restaurant_id");

            migrationBuilder.RenameIndex(
                name: "IX_categories_menu_id",
                table: "categories",
                newName: "IX_categories_restaurant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_restaurants_restaurant_id",
                table: "categories",
                column: "restaurant_id",
                principalTable: "restaurants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_menu_items_restaurants_restaurant_id",
                table: "menu_items",
                column: "restaurant_id",
                principalTable: "restaurants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
