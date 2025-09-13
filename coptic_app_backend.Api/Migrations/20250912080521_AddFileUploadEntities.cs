using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coptic_app_backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUploadEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileUploads",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FolderId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AbuneId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UploadedAt = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UploadSessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsChunkedUpload = table.Column<bool>(type: "boolean", nullable: false),
                    LastModified = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileUploads_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileUploads_Users_AbuneId",
                        column: x => x.AbuneId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileUploads_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalSize = table.Column<long>(type: "bigint", nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    CompletedChunks = table.Column<int>(type: "integer", nullable: false),
                    FolderId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AbuneId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    LastActivity = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MinioUploadId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedChunkETags = table.Column<Dictionary<int, string>>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MediaType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Users_AbuneId",
                        column: x => x.AbuneId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_AbuneId",
                table: "FileUploads",
                column: "AbuneId");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_FolderId",
                table: "FileUploads",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_IsActive",
                table: "FileUploads",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_MediaType",
                table: "FileUploads",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_Status",
                table: "FileUploads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_UploadedAt",
                table: "FileUploads",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_UploadedBy",
                table: "FileUploads",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploads_UploadSessionId",
                table: "FileUploads",
                column: "UploadSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_AbuneId",
                table: "UploadSessions",
                column: "AbuneId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_CreatedAt",
                table: "UploadSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_FolderId",
                table: "UploadSessions",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_LastActivity",
                table: "UploadSessions",
                column: "LastActivity");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_MediaType",
                table: "UploadSessions",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_Status",
                table: "UploadSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_UploadedBy",
                table: "UploadSessions",
                column: "UploadedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileUploads");

            migrationBuilder.DropTable(
                name: "UploadSessions");
        }
    }
}
