using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qwek.Migrations
{
    /// <inheritdoc />
    public partial class AddServerInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "invite_code",
                table: "servers",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE servers SET invite_code = " +
                "upper(substr(md5(random()::text || id::text), 1, 8)) " +
                "WHERE invite_code IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "invite_code",
                table: "servers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_servers_invite_code",
                table: "servers",
                column: "invite_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_servers_invite_code",
                table: "servers");

            migrationBuilder.DropColumn(
                name: "invite_code",
                table: "servers");
        }
    }
}
