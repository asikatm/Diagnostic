using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diagnostic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrainingSession1Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultVacutainerId",
                schema: "diag",
                table: "TestItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConsumable",
                schema: "diag",
                table: "TestItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFasting",
                schema: "diag",
                table: "TestItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfSample",
                schema: "diag",
                table: "TestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProcessTime",
                schema: "diag",
                table: "TestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TestSubGroupId",
                schema: "diag",
                table: "TestItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovementOrder",
                schema: "diag",
                table: "TestGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MovementRoomNo",
                schema: "diag",
                table: "TestGroups",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenOrder",
                schema: "diag",
                table: "TestGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FontSize",
                schema: "diag",
                table: "ReportParameters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBold",
                schema: "diag",
                table: "ReportParameters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGroupTitle",
                schema: "diag",
                table: "ReportParameters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsItalic",
                schema: "diag",
                table: "ReportParameters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalRangeChild",
                schema: "diag",
                table: "ReportParameters",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                schema: "diag",
                table: "ReportParameters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                schema: "diag",
                table: "ReportParameters",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestMethodId",
                schema: "diag",
                table: "ReportParameters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "diag",
                table: "Patients",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                schema: "diag",
                table: "InvestigationDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "diag",
                table: "InvestigationDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentDetailId",
                schema: "diag",
                table: "InvestigationDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                schema: "diag",
                table: "InvestigationDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TestItemConsumables",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestItemId = table.Column<int>(type: "int", nullable: false),
                    ConsumableItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestItemConsumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestItemConsumables_TestItems_ConsumableItemId",
                        column: x => x.ConsumableItemId,
                        principalSchema: "diag",
                        principalTable: "TestItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestItemConsumables_TestItems_TestItemId",
                        column: x => x.TestItemId,
                        principalSchema: "diag",
                        principalTable: "TestItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestMethods",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_TestMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestSubGroups",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestGroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TestSubGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestSubGroups_TestGroups_TestGroupId",
                        column: x => x.TestGroupId,
                        principalSchema: "diag",
                        principalTable: "TestGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestItems_DefaultVacutainerId",
                schema: "diag",
                table: "TestItems",
                column: "DefaultVacutainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TestItems_TestSubGroupId",
                schema: "diag",
                table: "TestItems",
                column: "TestSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportParameters_TestMethodId",
                schema: "diag",
                table: "ReportParameters",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_TestItemConsumables_ConsumableItemId",
                schema: "diag",
                table: "TestItemConsumables",
                column: "ConsumableItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TestItemConsumables_TestItemId_ConsumableItemId",
                schema: "diag",
                table: "TestItemConsumables",
                columns: new[] { "TestItemId", "ConsumableItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TestMethods_Code",
                schema: "diag",
                table: "TestMethods",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TestSubGroups_Code",
                schema: "diag",
                table: "TestSubGroups",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TestSubGroups_TestGroupId",
                schema: "diag",
                table: "TestSubGroups",
                column: "TestGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportParameters_TestMethods_TestMethodId",
                schema: "diag",
                table: "ReportParameters",
                column: "TestMethodId",
                principalSchema: "diag",
                principalTable: "TestMethods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestItems_TestSubGroups_TestSubGroupId",
                schema: "diag",
                table: "TestItems",
                column: "TestSubGroupId",
                principalSchema: "diag",
                principalTable: "TestSubGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestItems_Vacutainers_DefaultVacutainerId",
                schema: "diag",
                table: "TestItems",
                column: "DefaultVacutainerId",
                principalSchema: "diag",
                principalTable: "Vacutainers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportParameters_TestMethods_TestMethodId",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropForeignKey(
                name: "FK_TestItems_TestSubGroups_TestSubGroupId",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TestItems_Vacutainers_DefaultVacutainerId",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropTable(
                name: "TestItemConsumables",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "TestMethods",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "TestSubGroups",
                schema: "diag");

            migrationBuilder.DropIndex(
                name: "IX_TestItems_DefaultVacutainerId",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropIndex(
                name: "IX_TestItems_TestSubGroupId",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropIndex(
                name: "IX_ReportParameters_TestMethodId",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "DefaultVacutainerId",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropColumn(
                name: "IsConsumable",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropColumn(
                name: "IsFasting",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropColumn(
                name: "NumberOfSample",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropColumn(
                name: "ProcessTime",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropColumn(
                name: "TestSubGroupId",
                schema: "diag",
                table: "TestItems");

            migrationBuilder.DropColumn(
                name: "MovementOrder",
                schema: "diag",
                table: "TestGroups");

            migrationBuilder.DropColumn(
                name: "MovementRoomNo",
                schema: "diag",
                table: "TestGroups");

            migrationBuilder.DropColumn(
                name: "TokenOrder",
                schema: "diag",
                table: "TestGroups");

            migrationBuilder.DropColumn(
                name: "FontSize",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "IsBold",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "IsGroupTitle",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "IsItalic",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "NormalRangeChild",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "Remarks",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "ShortName",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "TestMethodId",
                schema: "diag",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "diag",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IsUrgent",
                schema: "diag",
                table: "InvestigationDetails");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "diag",
                table: "InvestigationDetails");

            migrationBuilder.DropColumn(
                name: "ParentDetailId",
                schema: "diag",
                table: "InvestigationDetails");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "diag",
                table: "InvestigationDetails");
        }
    }
}
