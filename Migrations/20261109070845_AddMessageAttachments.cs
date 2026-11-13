using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qwek.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attachment_content_type",
                table: "messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attachment_name",
                table: "messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "attachment_size",
                table: "messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attachment_url",
                table: "messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachment_content_type",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "attachment_name",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "attachment_size",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "attachment_url",
                table: "messages");
        }
    }
}
