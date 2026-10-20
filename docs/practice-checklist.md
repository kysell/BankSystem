# Реестр практики DEX C#/.NET — аудит 2026-10-20

Источник сверки: **полный конспект №121 из сообщения пользователя**. Отдельный оригинал PDF/TXT в рабочей папке не найден. Нумерация 26 — группировка практики и самостоятельных упражнений прежнего реестра, а не нумерация самого конспекта. Теоретические вопросы требуют самостоятельного понимания; выполнение кода не подтверждает усвоение материала.

Финальные проверки: обычные `dotnet restore/build/test` успешны; build — 0 предупреждений/ошибок; **67/67 NUnit, 0 пропусков**, дополнительно **3/3 HTTP-теста на PostgreSQL**. Newman: 12 запросов/14 assertions против памяти и PostgreSQL. Настоящий Chrome проверил Swagger, Rider debugger остановился на breakpoint. Practice/live CurrencyService проверены; EF model без pending changes. Логи: artifacts/verification, TRX: artifacts/test-results.

| № | Тема / требования | Фактическая реализация | Проверки / остаток | Зависимости; автозавершение | Статус |
|---|---|---|---|---|---|
| 1 | ООП: наследование, инкапсуляция, полиморфизм, интерфейс | Domain/Models.cs: Animal/Dog/Cat/IAnimal; Practice | Полиморфный запуск через IAnimal; объяснения в learning-guide | Нет; да | Выполнено |
| 2 | Person/Client/Employee, struct валюты, контракт, передача по значению/ref | Domain; BankService; Practice | LanguageFeatureTests, BoundaryTests; после задания * структура сохранена как CurrencyValue, Currency стала entity | Нет; да | Выполнено |
| 3 | Зарплата владельцев, Client→Employee, cast/as/is | BankService; Practice | BoundaryTests: зарплата, нет владельцев, mapping; sibling cast заменён корректным mapping | Нет; да | Выполнено |
| 4 | Отдельные Stopwatch замеры boxing/unboxing | Practice/Program.cs | Сохраняются boxed objects и итоговая сумма, работа не выбрасывается JIT; демо запускается | Нет; да | Выполнено |
| 5 | 1000 клиентов/сотрудников, телефонный Dictionary, List/key/LINQ, делегаты, Bogus | TestDataGenerator, DelegateExamples; Practice | BoundaryTests, LanguageFeatureTests; одинаковые данные для поиска, min salary, возраст; случайные наборы с seed | Bogus NuGet; да | Выполнено |
| 6 | INotifyPropertyChanged, очередь >n/пустая, процентные события | ObservableBalance, ThresholdQueue, NumberStreamAnalyzer | LanguageFeatureTests/RegressionTests; подписка/отписка в Practice, в том числе переход от нуля | Нет; да | Выполнено |
| 7 | Equals/GetHashCode, Client→несколько счетов, List<Employee> | Domain; CreateClientAccounts | EquivalenceTests, RegressionTests, BoundaryTests: новый равный ключ находит счета, List находит Employee по Id | Нет; да | Выполнено |
| 8 | CompareTo/IComparer, 10 фигур по убыванию, сортировка без CompareTo | Circle, CircleAreaDescendingComparer; Practice | LanguageFeatureTests, запуск 10 объектов; OrderBy по ключу | Нет; да | Выполнено |
| 9 | Оба хранилища, младший/старший/средний возраст | ClientStorage, EmployeeStorage | LanguageFeatureTests и RegressionTests; snapshot коллекций под блокировкой | Нет; да | Выполнено |
| 10 | Client/Employee validation, 18+, паспорт, default USD, CRUD счетов/фильтры | ClientService/EmployeeService, PersonValidation, RequestValidators | ClientServiceTests, BoundaryTests, ClientDbStorageTests, HTTP: ошибки и корректные пути | SQLite только для тестов; да | Выполнено |
| 11 | Generic storage, IClientStorage, DI, bonus/blacklist, зависимости | Abstractions.cs; BankService; ссылки csproj | BoundaryTests; Data→App→Domain, сервисы принимают интерфейсы | Нет; да | Выполнено |
| 12 | PostgreSQL DDL, PK/FK, один-ко-многим | sql/banksystem.sql | Реальный PostgreSQL 17: schema, INSERT, FK probe прошли | Изолированный /tmp кластер; остановлен | Выполнено |
| 13 | SQL INSERT, low/min/sum/join/age/group/limit | sql/banksystem.sql | Все запросы выполнены PostgreSQL; результаты в логе | Автоматически | Выполнено |
| 14 | EF, миграции up/down/up, DB CRUD, Currency* | Data, tools/verify-postgres.sh | Реальный PostgreSQL: миграции с сохранением данных, storage/CSV, API обеих сущностей прошли | Временный кластер не затрагивает рабочие базы | Выполнено |
| 15 | IDisposable/using, MemoryWorker | Resources.cs: SafeHandle, счётчики, идемпотентный Dispose | ConcurrencyAndResourceTests, RegressionTests: повторный Dispose/после Dispose | Нет; да | Выполнено |
| 16 | CSV export/import клиентов и добавление в БД | CsvHelper 33.1.0; ExportService; импорт через ClientService | ExportTests/RegressionTests и ClientDbStorageTests: кавычки, CRLF, заголовки, DB round-trip; PostgreSQL отдельно в №14 | SQLite round-trip реально выполнен; да | Выполнено |
| 17 | String extensions и int→TimeSpan | StringExtensions | LanguageFeatureTests; ограничения объяснены в guide | Нет; да | Выполнено |
| 18 | Reflection: public properties, создание по имени, private member, вызов с параметрами | ReflectionTools | LanguageFeatureTests/RegressionTests: assembly-qualified name, void/null, missing member | Нет; да | Выполнено |
| 19 | Конвейер, размер файла, чтение; 2 потока ×10×100 | ClientFilePipeline; Account | RegressionTests: bounded Channel, ротация по UTF-8 bytes, счета; BoundaryTests: 2 Thread + Barrier | Нет; да | Выполнено |
| 20 | Месячный фон, N списаний, async сервисы/хранилища, cancellation | RateUpdaterService.RunAsync, WithdrawManyAsync; async EF | ConcurrencyAndResourceTests/ClientDbStorageTests; MonthlyScheduleTests подтверждает границу месяца с виртуальным календарём | Нет; да, фоновый цикл запускается явно | Выполнено |
| 21 | Generic JSON Client/Employee, вложенные счета | ExportService; JsonInclude на коллекции/балансе | ExportTests/RegressionTests: Id, amount, currency и employee восстанавливаются | System.Text.Json; да | Выполнено |
| 22 | API, DTO, DI, validation, Swagger/debug | Api; App services/validators | Kestrel CRUD; Chrome отрисовал 10 операций, Try it out POST 201; Rider breakpoint: стек и DTO просмотрены | Автоматически, серверы остановлены | Выполнено |
| 23 | Team auth/add/get и Currency HTTP | ExternalServices; HttpContractTests | Реальный DEX OpenAPI прочитан; FoundationYear исправлен на int; anonymous GET=401; HTTPS certificate invalid; fake auth/add/get/errors пройдены | Для live auth нужен корректный HTTPS endpoint и собственная учебная учётка | Частично |
| 24 | Postman: environment, CRUD chain, variables/tests/logs | postman/*.json; tools/package.json | Настоящий Newman: 12 requests / 14 assertions / 0 failures; GUI не требуется повторно | Локальная npm dev-зависимость; системная установка не нужна | Выполнено |
| 25 | Regex IsMatch/Matches/Replace | TextPatterns | LanguageFeatureTests, positive и обработка duplicate words | Нет; да | Выполнено |
| 26 | Unit tests и калькулятор | BankSystem.App.Tests; SimpleCalculator | NUnit, границы/ошибки; есть отдельно API/SQLite integration tests; проверки не отключались | NUnit/Test SDK; да | Выполнено |

Итого: **26: выполнено 25, частично 1 (DEX live), заблокировано 0, не начато 0**. «Частично» включает подготовленный и проверенный локально код с оставшейся внешней/ручной проверкой.
