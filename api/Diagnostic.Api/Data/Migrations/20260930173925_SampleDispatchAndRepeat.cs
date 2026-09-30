using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diagnostic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SampleDispatchAndRepeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAt",
                schema: "diag",
                table: "SampleCollections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispatchedBy",
                schema: "diag",
                table: "SampleCollections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                schema: "diag",
                table: "SampleCollections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedBy",
                schema: "diag",
                table: "SampleCollections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatOfSampleId",
                schema: "diag",
                table: "SampleCollections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_RepeatOfSampleId",
                schema: "diag",
                table: "SampleCollections",
                column: "RepeatOfSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_Status",
                schema: "diag",
                table: "SampleCollections",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_SampleCollections_SampleCollections_RepeatOfSampleId",
                schema: "diag",
                table: "SampleCollections",
                column: "RepeatOfSampleId",
                principalSchema: "diag",
                principalTable: "SampleCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SampleCollections_SampleCollections_RepeatOfSampleId",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropIndex(
                name: "IX_SampleCollections_RepeatOfSampleId",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropIndex(
                name: "IX_SampleCollections_Status",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropColumn(
                name: "DispatchedAt",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropColumn(
                name: "DispatchedBy",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropColumn(
                name: "RepeatOfSampleId",
                schema: "diag",
                table: "SampleCollections");
        }
    }
}
