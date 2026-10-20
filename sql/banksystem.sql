-- Run only in a dedicated training database. This schema is separate from EF's public tables.
-- psql -v ON_ERROR_STOP=1 -d banksystem_training_NAME -f sql/banksystem.sql
begin;
create schema if not exists banksystem_sql_practice;
set local search_path = banksystem_sql_practice;
create table if not exists client (
  id uuid primary key, first_name varchar(100) not null, last_name varchar(100) not null,
  birth_date date not null, phone_number varchar(30) not null unique, passport_number varchar(50) not null unique
);
create table if not exists employee (
  id uuid primary key, first_name varchar(100) not null, last_name varchar(100) not null,
  birth_date date not null, phone_number varchar(30) not null unique, passport_number varchar(50) not null unique,
  salary integer not null check (salary >= 0)
);
create table if not exists account (
  id uuid primary key, client_id uuid not null references client(id) on delete cascade,
  currency_name varchar(3) not null, amount numeric(18,2) not null check (amount >= 0)
);
insert into client values
 ('00000000-0000-0000-0000-000000000001','Ada','Lovelace','1990-01-01','1001','P1'),
 ('00000000-0000-0000-0000-000000000002','Grace','Hopper','1980-01-01','1002','P2'),
 ('00000000-0000-0000-0000-000000000003','Alan','Turing','1990-02-01','1003','P3')
on conflict (id) do nothing;
insert into employee values
 ('00000000-0000-0000-0000-000000000004','Test','Employee','1985-01-01','2001','E1',2000)
on conflict (id) do nothing;
insert into account values
 ('00000000-0000-0000-0000-000000000011','00000000-0000-0000-0000-000000000001','USD',100),
 ('00000000-0000-0000-0000-000000000012','00000000-0000-0000-0000-000000000002','USD',50),
 ('00000000-0000-0000-0000-000000000013','00000000-0000-0000-0000-000000000001','USD',200)
on conflict (id) do nothing;
-- A client may have many accounts. An unknown owner is rejected (subtransaction rolls back the probe).
do $$ begin
 begin
  insert into account values ('00000000-0000-0000-0000-000000000099','ffffffff-ffff-ffff-ffff-ffffffffffff','USD',1);
  raise exception 'Foreign key check failed: invalid owner accepted';
 exception when foreign_key_violation then raise notice 'Foreign key correctly rejected an unknown client';
 end;
end $$;
-- b: Clients with balances below a limit, ascending.
select c.first_name, a.amount from client c join account a on a.client_id=c.id where a.amount < 150 order by a.amount;
-- c: Client with minimum account balance.
select c.first_name, a.amount from client c join account a on a.client_id=c.id order by a.amount, c.id limit 1;
-- d: Total per currency (summing different currencies requires conversion).
select currency_name, sum(amount) as total from account group by currency_name;
-- e: Accounts with owners.
select c.first_name, c.last_name, a.id, a.currency_name, a.amount from client c join account a on a.client_id=c.id;
-- f: Oldest to youngest.
select * from client order by birth_date, id;
-- g: Count clients of the same age.
select extract(year from age(current_date, birth_date))::int as age, count(*) from client group by age order by age;
-- h: Group by age, retaining client names.
select extract(year from age(current_date, birth_date))::int as age, array_agg(first_name order by first_name) as names from client group by age order by age;
-- i: Only N people.
select * from client order by last_name, id limit 2;
commit;
