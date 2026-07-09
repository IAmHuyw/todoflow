using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTasksAndSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceParentId",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceType",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_Status_SortOrder",
                table: "Tasks",
                columns: new[] { "UserId", "Status", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_Status_SortOrder",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceParentId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceType",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Tasks");
        }
    }
}
