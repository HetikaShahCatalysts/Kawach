using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Participant",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participant", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSession",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    AssessmentCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    StartedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSession", x => x.Id);
                    table.UniqueConstraint("AK_AssessmentSession_AssessmentId", x => x.AssessmentId);
                    table.ForeignKey(
                        name: "FK_AssessmentSession_Participant_UserId",
                        column: x => x.UserId,
                        principalTable: "Participant",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentAnswer",
                columns: table => new
                {
                    AnswerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnsweredOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAnswer", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK_AssessmentAnswer_AssessmentSession_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "AssessmentSession",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResult",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RiskLevel = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    DecisionPathway = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResult_AssessmentSession_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "AssessmentSession",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentStepTracking",
                columns: table => new
                {
                    TrackingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StepCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PageVersion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ClientOccurredOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentStepTracking", x => x.TrackingId);
                    table.ForeignKey(
                        name: "FK_AssessmentStepTracking_AssessmentSession_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "AssessmentSession",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswer_AssessmentId",
                table: "AssessmentAnswer",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswer_UserId_AssessmentId",
                table: "AssessmentAnswer",
                columns: new[] { "UserId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResult_AssessmentId",
                table: "AssessmentResult",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSession_UserId",
                table: "AssessmentSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentStepTracking_AssessmentId",
                table: "AssessmentStepTracking",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentStepTracking_UserId_AssessmentId",
                table: "AssessmentStepTracking",
                columns: new[] { "UserId", "AssessmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentAnswer");

            migrationBuilder.DropTable(
                name: "AssessmentResult");

            migrationBuilder.DropTable(
                name: "AssessmentStepTracking");

            migrationBuilder.DropTable(
                name: "AssessmentSession");

            migrationBuilder.DropTable(
                name: "Participant");
        }
    }
}
