using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TextToSpeech.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.CreateTable(
                name: "BackgroundJob",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputVersion = table.Column<int>(type: "integer", nullable: false),
                    InputId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputFingerprint = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    CurrentAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationRequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobAttempt",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobAttempt_BackgroundJob_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "BackgroundJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobEvent",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobEvent_BackgroundJob_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "BackgroundJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_BackgroundJob_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "BackgroundJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_Hash_OwnerId_TtsApiId_Status",
                table: "AudioFiles",
                columns: new[] { "Hash", "OwnerId", "TtsApiId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJob_OwnerId_JobType_IdempotencyKey",
                schema: "jobs",
                table: "BackgroundJob",
                columns: new[] { "OwnerId", "JobType", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJob_Status_LeaseExpiresAt",
                schema: "jobs",
                table: "BackgroundJob",
                columns: new[] { "Status", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobAttempt_JobId_Number",
                schema: "jobs",
                table: "BackgroundJobAttempt",
                columns: new[] { "JobId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobEvent_JobId",
                schema: "jobs",
                table: "BackgroundJobEvent",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_JobId",
                schema: "jobs",
                table: "OutboxMessage",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_PublishedAt_AvailableAt",
                schema: "jobs",
                table: "OutboxMessage",
                columns: new[] { "PublishedAt", "AvailableAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobAttempt",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "BackgroundJobEvent",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "BackgroundJob",
                schema: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_Hash_OwnerId_TtsApiId_Status",
                table: "AudioFiles");
        }
    }
}
