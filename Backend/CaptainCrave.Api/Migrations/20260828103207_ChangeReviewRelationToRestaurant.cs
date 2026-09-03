using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptainCrave.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeReviewRelationToRestaurant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_orders_order_id",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_order_id",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_user_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "reviews");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_user_id_restaurant_id",
                table: "reviews",
                columns: new[] { "user_id", "restaurant_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reviews_user_id_restaurant_id",
                table: "reviews");

            migrationBuilder.AddColumn<int>(
                name: "order_id",
                table: "reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_order_id",
                table: "reviews",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_user_id",
                table: "reviews",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_orders_order_id",
                table: "reviews",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
