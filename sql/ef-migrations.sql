CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020092316_InitialBankSystem') THEN
    CREATE TABLE client (
        id uuid NOT NULL,
        first_name character varying(100) NOT NULL,
        last_name character varying(100) NOT NULL,
        birth_date date NOT NULL,
        phone_number character varying(30) NOT NULL,
        passport_number character varying(50) NOT NULL,
        bonus numeric(18,2) NOT NULL,
        CONSTRAINT "PK_client" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020092316_InitialBankSystem') THEN
    CREATE TABLE employee (
        id uuid NOT NULL,
        position character varying(100) NOT NULL,
        salary integer NOT NULL,
        contract character varying(500) NOT NULL,
        is_bank_owner boolean NOT NULL,
        first_name character varying(100) NOT NULL,
        last_name character varying(100) NOT NULL,
        birth_date date NOT NULL,
        phone_number character varying(30) NOT NULL,
        passport_number character varying(50) NOT NULL,
        bonus numeric(18,2) NOT NULL,
        CONSTRAINT "PK_employee" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020092316_InitialBankSystem') THEN
    CREATE TABLE account (
        id uuid NOT NULL,
        amount numeric(18,2) NOT NULL,
        client_id uuid,
        currency_code character varying(3) NOT NULL,
        exchange_rate numeric(18,6) NOT NULL,
        CONSTRAINT "PK_account" PRIMARY KEY (id),
        CONSTRAINT "FK_account_client_client_id" FOREIGN KEY (client_id) REFERENCES client (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020092316_InitialBankSystem') THEN
    CREATE INDEX "IX_account_client_id" ON account (client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020092316_InitialBankSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261020092316_InitialBankSystem', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020104833_EnforceAccountOwnership') THEN
    DO $$ BEGIN IF EXISTS (SELECT 1 FROM account WHERE client_id IS NULL) THEN RAISE EXCEPTION 'Assign owners to orphan accounts before migrating'; END IF; END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020104833_EnforceAccountOwnership') THEN
    ALTER TABLE account ALTER COLUMN client_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020104833_EnforceAccountOwnership') THEN
    ALTER TABLE employee ADD CONSTRAINT ck_employee_salary CHECK (salary >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020104833_EnforceAccountOwnership') THEN
    ALTER TABLE account ADD CONSTRAINT ck_account_amount CHECK (amount >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020104833_EnforceAccountOwnership') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261020104833_EnforceAccountOwnership', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020105601_NormalizeCurrencies') THEN
    CREATE TABLE currency (
        code character varying(3) NOT NULL,
        exchange_rate numeric(18,6) NOT NULL,
        CONSTRAINT "PK_currency" PRIMARY KEY (code),
        CONSTRAINT ck_currency_rate CHECK (exchange_rate > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020105601_NormalizeCurrencies') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM account WHERE exchange_rate <= 0) OR
         EXISTS (SELECT currency_code FROM account GROUP BY currency_code HAVING min(exchange_rate) <> max(exchange_rate))
      THEN RAISE EXCEPTION 'Resolve invalid/conflicting currency rates before migrating'; END IF;
    END $$;
    INSERT INTO currency (code, exchange_rate)
    SELECT currency_code, min(exchange_rate) FROM account GROUP BY currency_code;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020105601_NormalizeCurrencies') THEN
    ALTER TABLE account DROP COLUMN exchange_rate;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020105601_NormalizeCurrencies') THEN
    CREATE INDEX "IX_account_currency_code" ON account (currency_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020105601_NormalizeCurrencies') THEN
    ALTER TABLE account ADD CONSTRAINT "FK_account_currency_currency_code" FOREIGN KEY (currency_code) REFERENCES currency (code) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020105601_NormalizeCurrencies') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261020105601_NormalizeCurrencies', '10.0.4');
    END IF;
END $EF$;
COMMIT;

