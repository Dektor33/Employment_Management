using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "ApplicationUsers");
        }
    }
}
