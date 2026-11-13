using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RealTimeApps.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reply_to_message_id",
                table: "messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "message_reactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<int>(type: "integer", nullable: false),
                    personal_user_id = table.Column<string>(type: "text", nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_reactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_reactions_asp_net_users_personal_user_id",
                        column: x => x.personal_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_reactions_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_messages_reply_to_message_id",
                table: "messages",
                column: "reply_to_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_reactions_message_id_personal_user_id_emoji",
                table: "message_reactions",
                columns: new[] { "message_id", "personal_user_id", "emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_reactions_personal_user_id",
                table: "message_reactions",
                column: "personal_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_messages_messages_reply_to_message_id",
                table: "messages",
                column: "reply_to_message_id",
                principalTable: "messages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_messages_reply_to_message_id",
                table: "messages");

            migrationBuilder.DropTable(
                name: "message_reactions");

            migrationBuilder.DropIndex(
                name: "ix_messages_reply_to_message_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "reply_to_message_id",
                table: "messages");
        }
    }
}
