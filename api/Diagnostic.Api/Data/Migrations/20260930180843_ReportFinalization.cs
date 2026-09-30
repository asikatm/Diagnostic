using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diagnostic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReportFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                schema: "diag",
                table: "LabReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalizedBy",
                schema: "diag",
                table: "LabReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                schema: "diag",
                table: "LabReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                schema: "diag",
                table: "LabReports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                schema: "diag",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "FinalizedBy",
                schema: "diag",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                schema: "diag",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                schema: "diag",
                table: "LabReports");
        }
    }
}
