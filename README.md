# BankSystem — учебный проект C#/.NET 10

Небольшая банковская система для изучения C#, ASP.NET Core, EF Core и PostgreSQL. Можно создать клиента, получить его данные, изменить их, удалить запись и выполнить поиск. У нового клиента появляется долларовый счёт. Для сотрудников реализован отдельный CRUD. Есть экспорт CSV/JSON и примеры LINQ, событий, потоков и async/await.

Проект дорабатывался с помощью Codex. Он предназначен для изучения и технической демонстрации; автору демонстрации нужно самостоятельно понимать код и уметь объяснить решения. Это учебный API без аутентификации, а не готовая система для реальных банковских данных.

## Быстрый запуск

Требуется .NET 10 SDK. Из корня решения:

```bash
dotnet restore ConsoleApp4.sln
dotnet build ConsoleApp4.sln
dotnet test ConsoleApp4.sln
dotnet run --project Practice --no-build
dotnet run --project BankSystem.Api --no-build --urls http://localhost:5000
```

Откройте http://localhost:5000/swagger. Swagger UI работает локально, без внешнего CDN. Без строки подключения API хранит данные в памяти. Для остановки нажмите Ctrl+C в терминале API.

Пример создания клиента:

```bash
curl -i http://localhost:5000/api/clients \
  -H 'Content-Type: application/json' \
  -d '{"firstName":"Ada","lastName":"Student","birthDate":"1990-01-01","phoneNumber":"123","passportNumber":"P123"}'
```

Ожидается `201 Created`, идентификатор клиента и один USD-счёт. GET `/api/clients/{id}` возвращает 200, PUT — 204, DELETE — 204, GET удалённого клиента — 404. Невалидные данные дают 400. Маршруты сотрудников: `/api/employees`.

Если среда ограничивает многопроцессный MSBuild, используйте `-m:1 -nr:false -p:UseSharedCompilation=false` для build/test.

## PostgreSQL без изменения рабочих баз

На macOS при уже установленном PostgreSQL и `dotnet-ef`:

```bash
bash tools/verify-postgres.sh
```

Скрипт создаёт отдельный каталог в `/tmp` и две собственные учебные базы на свободном loopback-порту. Проверяет SQL, миграции вверх/вниз/вверх с сохранением данных, CRUD, счета, CSV и API с PostgreSQL. Сервер останавливается при выходе, каталог с логами сохраняется. Рабочие базы и сервер на стандартном порту не используются. Системные компоненты скрипт не устанавливает.

Для своего постоянного **учебного** подключения задайте `ConnectionStrings__BankSystem` через Environment variables конфигурации запуска в Rider. API не применяет миграции автоматически. Секреты и личные строки подключения не добавляйте в исходники.

## Postman и Swagger: автоматические проверки

Для HTTP-проверки без npm-пакетов запустите API, затем:

```bash
node tools/run-postman.mjs http://localhost:5000
```

Это небольшой runner только для используемых в коллекции проверок. Для настоящего Newman и браузерной проверки Swagger:

```bash
npm ci --prefix tools --ignore-scripts
node tools/node_modules/newman/bin/newman.js run postman/BankSystem.postman_collection.json \
  -e postman/BankSystem.postman_environment.json
node tools/verify-swagger.mjs http://localhost:5000
```

Newman проверяет 12 запросов и 14 assertions. Проверка Swagger использует установленный Google Chrome в отдельном временном профиле; браузер не скачивается. Для другого расположения браузера задайте `CHROME_PATH`. Все npm-зависимости локальные и исключены из Git.

## Внешние HTTP-сервисы

```bash
dotnet run --project Practice --no-build -- --currency-smoke
```

Это необязательная проверка реального CurrencyService через Frankfurter. Основные тесты работают без внешней сети.

DEX-клиент проверен с подменёнными HTTP-ответами и сверен с публичной схемой. Для реальной авторизации нужен корректный HTTPS endpoint и собственная учебная учётка. При проверке старый HTTP endpoint возвращал 401 для защищённого запроса, а HTTPS-сертификат не прошёл проверку. Отправка паролей по HTTP и отключение проверки сертификата не используются. Подробности: [DEX](docs/dex-api-notes.md).

## Где читать код

| Проект | Назначение |
|---|---|
| BankSystem.Domain | Person, Client, Employee, Account, Currency и правила баланса |
| BankSystem.App | Сервисы, интерфейсы хранилищ, DTO, валидация и учебные примеры |
| BankSystem.Data | Хранение в памяти и EF Core/PostgreSQL, конфигурации и миграции |
| BankSystem.Api | HTTP-контроллеры, DI, обработка ошибок, OpenAPI/Swagger |
| ExportTool | CSV через CsvHelper и JSON через System.Text.Json |
| Practice | Консольные демонстрации и инфраструктурные smoke-проверки |
| BankSystem.App.Tests | NUnit: модульные проверки, SQLite, настоящий HTTP API |
| ConsoleApp4 | Сохранённый исходный консольный пример |

Начните с [подготовки к собеседованию](docs/interview-preparation.md). Подробные результаты находятся в [отчёте](docs/completion-report.md), задания — в [реестре](docs/practice-checklist.md), точка восстановления — в [codex-progress.md](docs/codex-progress.md).

Git инициализирован локально. Commit и push не выполнялись. `bin`, `obj`, `.idea`, отчёты, локальные базы, npm-пакеты и файлы секретов исключены через `.gitignore`. Текст учебного конспекта в репозиторий не добавлен.
