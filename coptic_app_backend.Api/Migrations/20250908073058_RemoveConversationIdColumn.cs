using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coptic_app_backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConversationIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop index if it exists
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_ChatMessages_ConversationId') THEN
                        DROP INDEX ""IX_ChatMessages_ConversationId"";
                    END IF;
                END $$;");

            // Drop column if it exists
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ChatMessages' AND column_name = 'ConversationId') THEN
                        ALTER TABLE ""ChatMessages"" DROP COLUMN ""ConversationId"";
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "ChatMessages",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");
        }
    }
}
