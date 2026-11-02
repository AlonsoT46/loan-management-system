using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlDeDinero.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentVoiding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "payments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "payments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAtUtc",
                table: "payments",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "VoidedAtUtc",
                table: "payments");
        }
    }
}
