using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlDeDinero.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuincenaInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InterestRate",
                table: "loans",
                newName: "InterestRatePerQuincena");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "loans",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quincenas",
                table: "loans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quincenas",
                table: "loans");

            migrationBuilder.RenameColumn(
                name: "InterestRatePerQuincena",
                table: "loans",
                newName: "InterestRate");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "loans",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");
        }
    }
}
