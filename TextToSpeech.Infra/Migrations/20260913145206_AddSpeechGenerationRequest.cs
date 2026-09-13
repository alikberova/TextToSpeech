using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TextToSpeech.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeechGenerationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "AudioFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "ContentId",
                table: "AudioFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SpeechGenerationRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeechGenerationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpeechGenerationRequest_BackgroundJob_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "BackgroundJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeechGenerationRequest_JobId",
                table: "SpeechGenerationRequest",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeechGenerationRequest");

            migrationBuilder.DropColumn(
                name: "ContentId",
                table: "AudioFiles");

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "AudioFiles",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
