#!/bin/bash
# Проверка создаёт только новый временный учебный кластер и останавливает его при выходе.
set -euo pipefail
bank_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$bank_root"
bank_pg_bin="$(pg_config --bindir)"
bank_dotnet_root="${DOTNET_ROOT:-$HOME/.dotnet}"
bank_ef="$HOME/.dotnet/tools/dotnet-ef"
if [ ! -x "$bank_pg_bin/initdb" ] || [ ! -x "$bank_ef" ]; then
  echo 'Existing PostgreSQL tools and dotnet-ef are required. Nothing has been installed.' >&2
  exit 1
fi
dotnet build ConsoleApp4.sln -m:1 -nr:false -p:UseSharedCompilation=false
bank_tmp="$(mktemp -d /tmp/banksystem-training.XXXXXX)"
bank_port="$(python3 - <<'PY'
import socket
with socket.socket() as s:
    s.bind(('127.0.0.1',0)); print(s.getsockname()[1])
PY
)"
bank_api_pid=''
bank_started=0
cleanup() {
  if [ -n "$bank_api_pid" ]; then kill -TERM "$bank_api_pid" 2>/dev/null || true; wait "$bank_api_pid" 2>/dev/null || true; fi
  if [ "$bank_started" = 1 ] && [ -f "$bank_tmp/data/postmaster.pid" ]; then "$bank_pg_bin/pg_ctl" -D "$bank_tmp/data" -m fast -w stop; fi
  echo "Training artifacts retained at $bank_tmp; temporary server stopped."
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
"$bank_pg_bin/initdb" -D "$bank_tmp/data" -U bankstudent --auth=trust --encoding=UTF8 --no-locale > "$bank_tmp/initdb.log"
bank_started=1
"$bank_pg_bin/pg_ctl" -D "$bank_tmp/data" -l "$bank_tmp/postgres.log" -o "-h 127.0.0.1 -p $bank_port -k $bank_tmp" -w start
for bank_database in banksystem_training_ef banksystem_training_sql; do
  "$bank_pg_bin/createdb" -h 127.0.0.1 -p "$bank_port" -U bankstudent "$bank_database"
done
export ConnectionStrings__BankSystem="Host=127.0.0.1;Port=$bank_port;Database=banksystem_training_ef;Username=bankstudent"
export BANKSYSTEM_ALLOW_TRAINING_DB=1
bank_psql=("$bank_pg_bin/psql" -X -v ON_ERROR_STOP=1 -h 127.0.0.1 -p "$bank_port" -U bankstudent)
bank_ef_run() { env DOTNET_ROOT="$bank_dotnet_root" "$bank_ef" "$@" --project BankSystem.Data --no-build; }
bank_ef_run database update InitialBankSystem
"${bank_psql[@]}" -d banksystem_training_ef <<'SQL'
insert into client values ('00000000-0000-0000-0000-000000000001','Migration','Probe','1990-01-01','migration-phone','migration-passport',0);
insert into account (id,amount,client_id,currency_code,exchange_rate) values ('00000000-0000-0000-0000-000000000011',100,'00000000-0000-0000-0000-000000000001','USD',1);
SQL
bank_ef_run database update
"${bank_psql[@]}" -d banksystem_training_ef <<'SQL'
do $$ begin
 if not exists (select 1 from currency where code='USD' and exchange_rate=1) or not exists (select 1 from account where amount=100) then raise exception 'Migration data preservation failed'; end if;
 begin
  insert into account(id,amount,client_id,currency_code) values ('00000000-0000-0000-0000-000000000099',1,null,'USD');
  raise exception 'Required FK was not enforced';
 exception when not_null_violation then raise notice 'Required FK verified'; end;
end $$;
-- Отдельные проверки отклоняют неверные данные и не оставляют их в базе.
do $$ begin
 begin
  update account set amount=-1;
  raise exception 'Negative balance accepted';
 exception when check_violation then raise notice 'Nonnegative balance verified'; end;
 begin
  update currency set exchange_rate=0;
  raise exception 'Zero currency rate accepted';
 exception when check_violation then raise notice 'Positive currency rate verified'; end;
 begin
  insert into account(id,amount,client_id,currency_code) values
   ('00000000-0000-0000-0000-000000000098',1,'ffffffff-ffff-ffff-ffff-ffffffffffff','USD');
  raise exception 'Unknown account owner accepted';
 exception when foreign_key_violation then raise notice 'Existing owner FK verified'; end;
 begin
  delete from currency where code='USD';
  raise exception 'Referenced currency deleted';
 exception when foreign_key_violation then raise notice 'Currency delete restriction verified'; end;
 begin
  insert into employee(id,first_name,last_name,birth_date,phone_number,passport_number,bonus,position,salary,contract,is_bank_owner)
  values ('00000000-0000-0000-0000-000000000097','Salary','Probe','1990-01-01','salary-phone','salary-passport',0,'Test',-1,'',false);
  raise exception 'Negative salary accepted';
 exception when check_violation then raise notice 'Nonnegative salary verified'; end;
end $$;
SQL
bank_ef_run database update EnforceAccountOwnership
"${bank_psql[@]}" -d banksystem_training_ef -c "DO \$\$ BEGIN IF (SELECT exchange_rate FROM account LIMIT 1) <> 1 THEN RAISE EXCEPTION 'Currency rollback lost data'; END IF; END \$\$;"
bank_ef_run database update
"${bank_psql[@]}" -d banksystem_training_sql -f sql/banksystem.sql
dotnet Practice/bin/Debug/net10.0/Practice.dll --postgres-smoke
# HTTP collection against the database-backed DI branch.
bank_api_port="$(python3 - <<'PY'
import socket
with socket.socket() as s:
    s.bind(('127.0.0.1',0)); print(s.getsockname()[1])
PY
)"
dotnet BankSystem.Api/bin/Debug/net10.0/BankSystem.Api.dll --urls "http://127.0.0.1:$bank_api_port" > "$bank_tmp/api.log" 2>&1 &
bank_api_pid=$!
for bank_attempt in {1..50}; do
  if curl -fsS "http://127.0.0.1:$bank_api_port/openapi/v1.json" -o "$bank_tmp/openapi.json" 2>/dev/null; then break; fi
  sleep .1
done
node tools/run-postman.mjs "http://127.0.0.1:$bank_api_port"
if [ -f tools/node_modules/newman/bin/newman.js ]; then
  node tools/node_modules/newman/bin/newman.js run postman/BankSystem.postman_collection.json \
    -e postman/BankSystem.postman_environment.json --env-var "baseUrl=http://127.0.0.1:$bank_api_port" \
    --reporters cli,json --reporter-json-export "$bank_tmp/newman.json"
fi
env BANKSYSTEM_HTTP_TEST_CONNECTION="$ConnectionStrings__BankSystem" \
  dotnet test BankSystem.App.Tests/BankSystem.App.Tests.csproj --no-build --no-restore \
  --filter 'FullyQualifiedName~ApiIntegrationTests' \
  --logger 'trx;LogFileName=PostgreSQL-HTTP.trx' --results-directory "$bank_tmp/test-results"
echo 'PostgreSQL migrations (up/down/up), SQL, storage and database-backed HTTP checks passed.' 
