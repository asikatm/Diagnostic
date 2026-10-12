using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diagnostic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaCommission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MediaCommission",
                schema: "diag",
                table: "Investigations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MediaId",
                schema: "diag",
                table: "Investigations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MediaCommission",
                schema: "diag",
                table: "InvestigationDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TestItemId",
                schema: "diag",
                table: "DiscountSetups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Medias",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Phone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DefaultCommissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaCommissionSetups",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MediaId = table.Column<int>(type: "int", nullable: true),
                    TestGroupId = table.Column<int>(type: "int", nullable: true),
                    TestItemId = table.Column<int>(type: "int", nullable: true),
                    CommissionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaCommissionSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaCommissionSetups_Medias_MediaId",
                        column: x => x.MediaId,
                        principalSchema: "diag",
                        principalTable: "Medias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaCommissionSetups_TestGroups_TestGroupId",
                        column: x => x.TestGroupId,
                        principalSchema: "diag",
                        principalTable: "TestGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaCommissionSetups_TestItems_TestItemId",
                        column: x => x.TestItemId,
                        principalSchema: "diag",
                        principalTable: "TestItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Investigations_MediaId",
                schema: "diag",
                table: "Investigations",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountSetups_TestItemId",
                schema: "diag",
                table: "DiscountSetups",
                column: "TestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCommissionSetups_MediaId_TestGroupId_TestItemId",
                schema: "diag",
                table: "MediaCommissionSetups",
                columns: new[] { "MediaId", "TestGroupId", "TestItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCommissionSetups_TestGroupId",
                schema: "diag",
                table: "MediaCommissionSetups",
                column: "TestGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCommissionSetups_TestItemId",
                schema: "diag",
                table: "MediaCommissionSetups",
                column: "TestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_Code",
                schema: "diag",
                table: "Medias",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_Phone",
                schema: "diag",
                table: "Medias",
                column: "Phone");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountSetups_TestItems_TestItemId",
                schema: "diag",
                table: "DiscountSetups",
                column: "TestItemId",
                principalSchema: "diag",
                principalTable: "TestItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Investigations_Medias_MediaId",
                schema: "diag",
                table: "Investigations",
                column: "MediaId",
                principalSchema: "diag",
                principalTable: "Medias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountSetups_TestItems_TestItemId",
                schema: "diag",
                table: "DiscountSetups");

            migrationBuilder.DropForeignKey(
                name: "FK_Investigations_Medias_MediaId",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropTable(
                name: "MediaCommissionSetups",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "Medias",
                schema: "diag");

            migrationBuilder.DropIndex(
                name: "IX_Investigations_MediaId",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropIndex(
                name: "IX_DiscountSetups_TestItemId",
                schema: "diag",
                table: "DiscountSetups");

            migrationBuilder.DropColumn(
                name: "MediaCommission",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropColumn(
                name: "MediaId",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropColumn(
                name: "MediaCommission",
                schema: "diag",
                table: "InvestigationDetails");

            migrationBuilder.DropColumn(
                name: "TestItemId",
                schema: "diag",
                table: "DiscountSetups");
        }
    }
}
