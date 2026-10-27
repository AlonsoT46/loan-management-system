using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlDeDinero.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DomainModelRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvent_loans_LoanId",
                table: "AuditEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_Installment_loans_LoanId",
                table: "Installment");

            migrationBuilder.DropForeignKey(
                name: "FK_Payment_loans_LoanId",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocation_Payment_PaymentId",
                table: "PaymentAllocation");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAllocation_loans_LoanId",
                table: "PaymentAllocation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentAllocation",
                table: "PaymentAllocation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payment",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_LoanId",
                table: "Payment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Installment",
                table: "Installment");

            migrationBuilder.DropIndex(
                name: "IX_Installment_LoanId",
                table: "Installment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditEvent",
                table: "AuditEvent");

            migrationBuilder.RenameTable(
                name: "PaymentAllocation",
                newName: "payment_allocations");

            migrationBuilder.RenameTable(
                name: "Payment",
                newName: "payments");

            migrationBuilder.RenameTable(
                name: "Installment",
                newName: "installments");

            migrationBuilder.RenameTable(
                name: "AuditEvent",
                newName: "audit_events");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentAllocation_PaymentId",
                table: "payment_allocations",
                newName: "IX_payment_allocations_PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentAllocation_LoanId",
                table: "payment_allocations",
                newName: "IX_payment_allocations_LoanId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "audit_events",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_AuditEvent_LoanId",
                table: "audit_events",
                newName: "IX_audit_events_LoanId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "loans",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "loans",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "clients",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "payments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "installments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "audit_events",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "audit_events",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payment_allocations",
                table: "payment_allocations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_installments",
                table: "installments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_events",
                table: "audit_events",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_loans_LoanDate",
                table: "loans",
                column: "LoanDate");

            migrationBuilder.CreateIndex(
                name: "IX_clients_Name",
                table: "clients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_payments_LoanId_PaymentDate",
                table: "payments",
                columns: new[] { "LoanId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_installments_LoanId_Number",
                table: "installments",
                columns: new[] { "LoanId", "Number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_events_loans_LoanId",
                table: "audit_events",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_installments_loans_LoanId",
                table: "installments",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_loans_LoanId",
                table: "payment_allocations",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_payments_PaymentId",
                table: "payment_allocations",
                column: "PaymentId",
                principalTable: "payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_loans_LoanId",
                table: "payments",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_events_loans_LoanId",
                table: "audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_installments_loans_LoanId",
                table: "installments");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_loans_LoanId",
                table: "payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_payments_PaymentId",
                table: "payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_loans_LoanId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_loans_LoanDate",
                table: "loans");

            migrationBuilder.DropIndex(
                name: "IX_clients_Name",
                table: "clients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_LoanId_PaymentDate",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payment_allocations",
                table: "payment_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_installments",
                table: "installments");

            migrationBuilder.DropIndex(
                name: "IX_installments_LoanId_Number",
                table: "installments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_events",
                table: "audit_events");

            migrationBuilder.RenameTable(
                name: "payments",
                newName: "Payment");

            migrationBuilder.RenameTable(
                name: "payment_allocations",
                newName: "PaymentAllocation");

            migrationBuilder.RenameTable(
                name: "installments",
                newName: "Installment");

            migrationBuilder.RenameTable(
                name: "audit_events",
                newName: "AuditEvent");

            migrationBuilder.RenameIndex(
                name: "IX_payment_allocations_PaymentId",
                table: "PaymentAllocation",
                newName: "IX_PaymentAllocation_PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_payment_allocations_LoanId",
                table: "PaymentAllocation",
                newName: "IX_PaymentAllocation_LoanId");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "AuditEvent",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_audit_events_LoanId",
                table: "AuditEvent",
                newName: "IX_AuditEvent_LoanId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "loans",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "loans",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "clients",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Payment",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Installment",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "AuditEvent",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AuditEvent",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payment",
                table: "Payment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentAllocation",
                table: "PaymentAllocation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Installment",
                table: "Installment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditEvent",
                table: "AuditEvent",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_LoanId",
                table: "Payment",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Installment_LoanId",
                table: "Installment",
                column: "LoanId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvent_loans_LoanId",
                table: "AuditEvent",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Installment_loans_LoanId",
                table: "Installment",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_loans_LoanId",
                table: "Payment",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocation_Payment_PaymentId",
                table: "PaymentAllocation",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocation_loans_LoanId",
                table: "PaymentAllocation",
                column: "LoanId",
                principalTable: "loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
