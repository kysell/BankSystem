# BankSystem 

Учебная банковская система на .NET 10: клиенты, сотрудники, счета, фильтры, PostgreSQL/EF Core, CSV/JSON, HTTP-конвертация валют и упражнения C#. Требования сверены с оригинальным PDF №121 из Downloads; источник, страницы и цитаты — в [requirements-audit.md](docs/requirements-audit.md).

В PDF нет единого списка из 26 заданий. Наш пересчёт: 18 самостоятельных блоков практики и 14 отдельных заданий с написанием кода в «Самостоятельно»; ещё 2 блока прямо названы дополнительными. Реализация есть для всех этих пунктов. Для полного воспроизведения процедуры Postman осталось вручную импортировать cURL из браузера и OpenAPI в Postman. Подробный статус — [practice-checklist.md](docs/practice-checklist.md).

Проект дорабатывался с помощью Codex. Код предназначен для изучения; перед собеседованием нужно самостоятельно объяснить решения. API учебный, без системы входа пользователей.

## Запуск

Нужен .NET 10 SDK. Из корня решения:

    dotnet restore ConsoleApp4.sln
    dotnet build ConsoleApp4.sln
    dotnet test ConsoleApp4.sln
    dotnet run --project Practice --no-build
    dotnet run --project ConsoleApp4 --no-build
    dotnet run --project BankSystem.Api --no-build --urls http://localhost:5000

Откройте http://localhost:5000/swagger. Для остановки API нажмите Ctrl+C. Без строки подключения используется хранилище в памяти.

GET/POST: /api/clients, /api/employees. GET/PUT/DELETE по идентификатору: /api/clients/{id}, /api/employees/{id}. Создание возвращает 201 и Location, изменение/удаление — 204, отсутствующий объект — 404, неверные данные — 400. Новому клиенту создаётся USD-счёт.

Пример тела POST /api/clients:

    {"firstName":"Ada","lastName":"Student","birthDate":"1990-01-01","phoneNumber":"123","passportNumber":"P123"}

## Проекты

| Проект | Что изучаем |
|---|---|
| BankSystem.Domain | Person, Client, Employee, Account; ООП и равенство; CurrencyValue — struct из раннего упражнения |
| BankSystem.App | BankService, ClientService, EmployeeService, RateUpdaterService, CurrencyService, интерфейсы, DTO и валидаторы |
| BankSystem.Data | Коллекции в памяти и EF-хранилища, DbContext, конфигурации, миграции |
| BankSystem.Api | Контроллеры клиентов/сотрудников, DI, ошибки, Swagger |
| ExportTool | CSV через CsvHelper, обобщённый JSON export/import |
| Practice | Консольные упражнения; отдельные проверки PostgreSQL и HTTP |
| ConsoleApp4 | Самостоятельный калькулятор с четырьмя операциями, стр. 142 |
| BankSystem.App.Tests | NUnit: сервисы, упражнения, экспорт, HTTP API и калькулятор |
| BankSystem.Data.Tests | NUnit: EF/SQLite и месячный сервис; отдельный проект предусмотрен на стр. 113 |

Продуктовые зависимости идут Data → App → Domain и Api → App/Data. Экспорт использует модели и ClientService. Проекты тестов и консольные запуски не являются новыми архитектурными слоями.

## PostgreSQL

При уже установленных PostgreSQL tools и dotnet-ef:

    bash tools/verify-postgres.sh

Скрипт создаёт новый изолированный кластер в /tmp на свободном loopback-порту; проверяет SQL, миграции вверх/вниз/вверх, CRUD, CSV и API. Рабочие базы не используются. При выходе сервер останавливается, диагностические файлы сохраняются. Системные программы не устанавливаются.

Для собственной учебной БД задайте ConnectionStrings__BankSystem в Environment variables конфигурации Rider. Затем примените миграции к этой базе:

    dotnet ef database update --project BankSystem.Data

API не меняет схему базы автоматически. Не храните личные строки подключения в исходниках.

## Postman

Коллекция и окружение лежат в postman/. Для запуска без GUI:

    npm ci --prefix tools --ignore-scripts
    node tools/node_modules/newman/bin/newman.js run postman/BankSystem.postman_collection.json -e postman/BankSystem.postman_environment.json

API должен работать на localhost:5000. Проверяются 12 запросов и 14 assertions. Для полного выполнения процедуры на стр. 134 дополнительно нужны импорты cURL и OpenAPI в Postman; короткие шаги — в [learning-guide.md](docs/learning-guide.md). Собственный Postman runtime и Playwright удалены.

## Внешний HTTP

    dotnet run --project Practice --no-build -- --currency-smoke

CurrencyService получает курс Frankfurter по HTTP API и вычисляет сумму; сетевые ошибки сообщаются пользователю. Amdoren в конспекте указан как возможный провайдер. DEX TeamService является примером автора, его интеграция удалена из проекта; аккаунт DEX для практики не нужен.

## Для изучения

[Карта требований](docs/requirements-audit.md), [результаты ревизии](docs/completion-report.md), [объяснение кода и вопросы](docs/interview-preparation.md), [точка восстановления](docs/codex-progress.md).

Локальный начальный commit: 73fb959. Ревизия выполняется в отдельной ветке; публикация в удалённый репозиторий не выполнялась. Исходный конспект не включён в Git. bin/obj, IDE-файлы, секреты, локальные базы, отчёты и npm-пакеты исключены через .gitignore.
