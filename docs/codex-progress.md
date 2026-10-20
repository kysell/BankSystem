# BankSystem — точка восстановления, 2026-10-20

## Актуальный итог

**26 заданий: 25 выполнено, 1 частично (№23: авторизованные live-вызовы DEX), 0 заблокировано, 0 не начато.**

Решение ConsoleApp4.sln, 8 проектов net10.0. SDK 10.0.302. Продолжены существующие файлы, решение не пересоздавалось. Исходный ConsoleApp4 и миграция InitialBankSystem сохранены. Оригинал конспекта не добавлен в репозиторий.

## Последние проверки

- Обычные `dotnet restore ConsoleApp4.sln`, `dotnet build ConsoleApp4.sln`, `dotnet test ConsoleApp4.sln` выполнены успешно вне ограничений sandbox.
- Финальный build: **0 предупреждений / 0 ошибок**.
- Финальный test: **67/67, 0 пропусков**. `artifacts/test-results/BankSystem.trx`.
- На настоящем PostgreSQL дополнительно **3/3 NUnit HTTP cases**. `artifacts/test-results/PostgreSQL-HTTP.trx`.
- PostgreSQL 17: Initial→latest→previous→latest с сохранением данных, SQL INSERT/JOIN/GROUP/агрегаты/limit, NOT NULL/FK/CHECK/Restrict, CRUD/CSV/счета/API.
- Newman: **12 запросов / 14 assertions / 0 failures** против памяти и PostgreSQL. Логи и JSON в artifacts/verification.
- Swagger в настоящем Chrome: 10 операций, Try it out POST 201 с USD-счётом, удаление 204. `artifacts/verification/swagger.png`.
- Rider debugger: реальный POST остановился на breakpoint в контроллере; стек и DTO просмотрены. Сессия остановлена, только мой breakpoint удалён. Финальный status: sessions=[].
- Practice: успешный запуск учебных примеров. EF has-pending-model-changes: No changes.
- Live CurrencyService: 10 USD→8.91 EUR, rate 0.89075. Один промежуточный timeout сохранён в currency-timeout.log; повторный C# запрос успешен. CLI теперь печатает понятную ошибку и возвращает 1 при сетевом сбое.
- `bash -n` и `node --check`: успешно.
- Все учебные API/Chrome/PostgreSQL процессы остановлены, проверено через ps и отсутствие postmaster.pid.

Последний временный кластер: `/tmp/banksystem-training.bfzY23`; первый: `/tmp/banksystem-training.F7AN04`. Оба остановлены; файлы оставлены для диагностики. Рабочие базы не использовались.

## Завершённые группы

1. **PostgreSQL:** пользователь прямо разрешил безопасный изолированный запуск. Проверен и выполнен tools/verify-postgres.sh; №12–14 завершены.
2. **API/Swagger/Postman/debug:** локальный Swagger UI через NuGet, без CDN. Настоящие Chrome/Newman/Rider проверки завершены; №22/24 завершены, ручной GUI для повторения не нужен.
3. **Учебный стиль:** обычные конструкторы/поля, развёрнутые методы, if/foreach, понятные имена. ClientService/EmployeeService/TestDataGenerator/RateUpdaterService/Events/LanguageExamples вынесены из большого Services.cs. Интерфейсы только описывают контракт, реализация — в хранилищах. DbContext явно подключает 4 конфигурации. LINQ/EF/DI/async/generics курса сохранены.
4. **CSV:** ручной парсер заменён CsvHelper 33.1.0; кавычки/CRLF/UTF-8/дубликаты/перезапись/DB-import тесты проходят. SafeHandle/TimeProvider/SemaphoreSlim сохранены ради корректности, не заменены упрощёнными небезопасными версиями.
5. **HTTP:** Team.FoundationYear исправлен с DateTime на int по реальному OpenAPI. Неудачная reauth очищает старый токен. HTTPS guard предотвращает отправку пароля по HTTP. HttpContractTests добавлены.
6. **Git и документация:** создан .gitignore, локальный Git develop, README, interview-preparation.md (8 проектов, маршрут создания клиента, 20 вопросов, объяснение за 2 минуты), dex-api-notes.md. Сигнатур секретов не обнаружено. Нет staging/commit/push/remote/публикации.

## Что изменено последним

Practice/Program.cs (обработка сетевой ошибки smoke), ApiIntegrationTests.cs (проверка настоящего Swagger и возможность защищённой PostgreSQL HTTP проверки), tools/verify-postgres.sh (constraints/Newman/NUnit/cleanup), все документы итогового состояния.

Новые учебные исходники: BankSystem.App/ClientService.cs, EmployeeService.cs, PersonValidation.cs, TestDataGenerator.cs, RateUpdaterService.cs, Events.cs, LanguageExamples.cs; BankSystem.Data/EmployeeDbStorage.cs; BankSystem.App.Tests/HttpContractTests.cs. Существующие исходники моделей, DbContext, storage, контроллеров, экспорта и тестов упрощены.

## Единственный внешний остаток и следующий шаг

DEX HTTP Swagger/OpenAPI доступен (200), GET Teams без авторизации — 401; HTTPS сертификат self-signed, обычная проверка TLS не прошла. Credentials не передавались, аккаунты/команды на внешнем сервере не создавались. Fake auth/add/get/errors и актуальный контракт протестированы.

Пользователю задан вопрос об актуальном корректном HTTPS endpoint, без запроса пароля/токена. Следующий конкретный шаг: при получении безопасного URL проверить сертификат и публичную схему. Авторизованный вызов выполнять только с собственной учебной учёткой, переданной пользователем локально безопасным способом; секреты не сохранять в код/логи/Git. Не использовать HTTP для пароля и не отключать TLS validation. До получения адреса дополнительных обязательных действий для локальной демонстрации нет.

Дальнейшую сессию начать с этого файла, practice-checklist и фактического Git status. Commit/push по-прежнему требуют отдельного разрешения.
