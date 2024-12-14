using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMR.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContactTagModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "ContactTags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ContactTags",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
