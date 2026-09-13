using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TextToSpeech.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioFileIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_Hash_OwnerId_TtsApiId_Status",
                table: "AudioFiles",
                columns: new[] { "Hash", "OwnerId", "TtsApiId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_Hash_OwnerId_TtsApiId_Status",
                table: "AudioFiles");
        }
    }
}
