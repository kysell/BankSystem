using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace BankSystem.Data.Migrations;

public partial class NormalizeCurrencies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "currency",
            columns: table => new
            {
                code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_currency", x => x.code);
                table.CheckConstraint("ck_currency_rate", "exchange_rate > 0");
            });
        // Preserve existing rates; refuse ambiguous reference data instead of choosing a rate silently.
        migrationBuilder.Sql("""
            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM account WHERE exchange_rate <= 0) OR
                 EXISTS (SELECT currency_code FROM account GROUP BY currency_code HAVING min(exchange_rate) <> max(exchange_rate))
              THEN RAISE EXCEPTION 'Resolve invalid/conflicting currency rates before migrating'; END IF;
            END $$;
            INSERT INTO currency (code, exchange_rate)
            SELECT currency_code, min(exchange_rate) FROM account GROUP BY currency_code;
            """);
        migrationBuilder.DropColumn(name: "exchange_rate", table: "account");
        migrationBuilder.CreateIndex(name: "IX_account_currency_code", table: "account", column: "currency_code");
        migrationBuilder.AddForeignKey(name: "FK_account_currency_currency_code", table: "account", column: "currency_code",
            principalTable: "currency", principalColumn: "code", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_account_currency_currency_code", table: "account");
        migrationBuilder.DropIndex(name: "IX_account_currency_code", table: "account");
        migrationBuilder.AddColumn<decimal>(name: "exchange_rate", table: "account", type: "numeric(18,6)",
            precision: 18, scale: 6, nullable: false, defaultValue: 1m);
        migrationBuilder.Sql("UPDATE account a SET exchange_rate = c.exchange_rate FROM currency c WHERE a.currency_code = c.code;");
        migrationBuilder.DropTable(name: "currency");
    }
}
