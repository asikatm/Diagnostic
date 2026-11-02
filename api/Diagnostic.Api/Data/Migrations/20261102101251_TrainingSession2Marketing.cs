using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diagnostic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrainingSession2Marketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarrierUserId",
                schema: "diag",
                table: "SampleCollections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountPercent",
                schema: "admin",
                table: "Roles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketingAreaId",
                schema: "diag",
                table: "Medias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInReport",
                schema: "diag",
                table: "LabResults",
                type: "bit",
                nullable: false,
                defaultValue: true); // existing results stay printed

            migrationBuilder.AddColumn<int>(
                name: "MarketingAreaId",
                schema: "diag",
                table: "Investigations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChamberAddress",
                schema: "diag",
                table: "Doctors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommissionGroupId",
                schema: "diag",
                table: "Doctors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentWorkPlace",
                schema: "diag",
                table: "Doctors",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                schema: "diag",
                table: "Doctors",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraAmount",
                schema: "diag",
                table: "Doctors",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsChamberPractitioner",
                schema: "diag",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsImagingReporter",
                schema: "diag",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSonologist",
                schema: "diag",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MediaId",
                schema: "diag",
                table: "Doctors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PastWorkPlace",
                schema: "diag",
                table: "Doctors",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecialityId",
                schema: "diag",
                table: "Doctors",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionGroups",
                schema: "diag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_CommissionGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSpecialities",
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
                    table.PrimaryKey("PK_DoctorSpecialities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingAreas",
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
                    table.PrimaryKey("PK_MarketingAreas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_CarrierUserId",
                schema: "diag",
                table: "SampleCollections",
                column: "CarrierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_MarketingAreaId",
                schema: "diag",
                table: "Medias",
                column: "MarketingAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Investigations_MarketingAreaId",
                schema: "diag",
                table: "Investigations",
                column: "MarketingAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_CommissionGroupId",
                schema: "diag",
                table: "Doctors",
                column: "CommissionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_MediaId",
                schema: "diag",
                table: "Doctors",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SpecialityId",
                schema: "diag",
                table: "Doctors",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionGroups_Code",
                schema: "diag",
                table: "CommissionGroups",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSpecialities_Code",
                schema: "diag",
                table: "DoctorSpecialities",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAreas_Code",
                schema: "diag",
                table: "MarketingAreas",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_CommissionGroups_CommissionGroupId",
                schema: "diag",
                table: "Doctors",
                column: "CommissionGroupId",
                principalSchema: "diag",
                principalTable: "CommissionGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_DoctorSpecialities_SpecialityId",
                schema: "diag",
                table: "Doctors",
                column: "SpecialityId",
                principalSchema: "diag",
                principalTable: "DoctorSpecialities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Medias_MediaId",
                schema: "diag",
                table: "Doctors",
                column: "MediaId",
                principalSchema: "diag",
                principalTable: "Medias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Investigations_MarketingAreas_MarketingAreaId",
                schema: "diag",
                table: "Investigations",
                column: "MarketingAreaId",
                principalSchema: "diag",
                principalTable: "MarketingAreas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_MarketingAreas_MarketingAreaId",
                schema: "diag",
                table: "Medias",
                column: "MarketingAreaId",
                principalSchema: "diag",
                principalTable: "MarketingAreas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SampleCollections_Users_CarrierUserId",
                schema: "diag",
                table: "SampleCollections",
                column: "CarrierUserId",
                principalSchema: "admin",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_CommissionGroups_CommissionGroupId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_DoctorSpecialities_SpecialityId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Medias_MediaId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Investigations_MarketingAreas_MarketingAreaId",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropForeignKey(
                name: "FK_Medias_MarketingAreas_MarketingAreaId",
                schema: "diag",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_SampleCollections_Users_CarrierUserId",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropTable(
                name: "CommissionGroups",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "DoctorSpecialities",
                schema: "diag");

            migrationBuilder.DropTable(
                name: "MarketingAreas",
                schema: "diag");

            migrationBuilder.DropIndex(
                name: "IX_SampleCollections_CarrierUserId",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropIndex(
                name: "IX_Medias_MarketingAreaId",
                schema: "diag",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Investigations_MarketingAreaId",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_CommissionGroupId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_MediaId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_SpecialityId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CarrierUserId",
                schema: "diag",
                table: "SampleCollections");

            migrationBuilder.DropColumn(
                name: "MaxDiscountPercent",
                schema: "admin",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "MarketingAreaId",
                schema: "diag",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "ShowInReport",
                schema: "diag",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "MarketingAreaId",
                schema: "diag",
                table: "Investigations");

            migrationBuilder.DropColumn(
                name: "ChamberAddress",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CommissionGroupId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CurrentWorkPlace",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Details",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "ExtraAmount",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "IsChamberPractitioner",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "IsImagingReporter",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "IsSonologist",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "MediaId",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "PastWorkPlace",
                schema: "diag",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "SpecialityId",
                schema: "diag",
                table: "Doctors");
        }
    }
}
