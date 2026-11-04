using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpCode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocLinksJson",
                table: "Problems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocLinksJson",
                table: "Problems");
        }
    }
}
