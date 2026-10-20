using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tandem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCateringIncludesTableware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CateringIncludesTableware",
                table: "Meals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Meals",
                keyColumn: "Id",
                keyValue: 1,
                column: "CateringIncludesTableware",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CateringIncludesTableware",
                table: "Meals");
        }
    }
}
