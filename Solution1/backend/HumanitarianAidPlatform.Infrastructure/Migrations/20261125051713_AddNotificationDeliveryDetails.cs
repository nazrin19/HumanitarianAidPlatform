using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanitarianAidPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryCompletedAt",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "Notifications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveryCompletedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
