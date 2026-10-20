using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAccountOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail without changing data if a legacy database contains orphan accounts.
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM account WHERE client_id IS NULL) THEN RAISE EXCEPTION 'Assign owners to orphan accounts before migrating'; END IF; END $$;");
            migrationBuilder.AlterColumn<Guid>(
                name: "client_id",
                table: "account",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_salary",
                table: "employee",
                sql: "salary >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_account_amount",
                table: "account",
                sql: "amount >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_salary",
                table: "employee");

            migrationBuilder.DropCheckConstraint(
                name: "ck_account_amount",
                table: "account");

            migrationBuilder.AlterColumn<Guid>(
                name: "client_id",
                table: "account",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
