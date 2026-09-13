using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TextToSpeech.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioFileOwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "AudioFiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_OwnerId",
                table: "AudioFiles",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_OwnerId",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "AudioFiles");
        }
    }
}
