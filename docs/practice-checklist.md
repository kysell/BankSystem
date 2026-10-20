# Статус практики после сверки с PDF №121

Источник и точные формулировки — [requirements-audit.md](requirements-audit.md). Оригинал PDF найден в Downloads, версия на стр. 143 — №121. Прежняя группировка из 26 пунктов больше не используется.

Правило счёта: 18 самостоятельных блоков «Практическая часть» + 14 отдельных поручений написать код в «Самостоятельно» = 32 основных пункта реализации. Два заголовка практики отсылают к другим главам и не считаются дважды. Дополнительно 2 необязательных блока O01/O02. Это наши идентификаторы для сверки, не авторская сквозная нумерация.

| ID | Результат | Статус и доказательство |
|---|---|---|
| P01 | Модели, контракт, struct по значению/ref | Выполнено: Domain, BankService, Practice, ValueType test |
| P02 | Зарплата владельцев и Client→Employee | Выполнено: BankService, Practice, тесты; невозможный sibling cast заменён явным копированием данных |
| P03 | 1000 клиентов/сотрудников, словарь, 4 замера поиска, LINQ и делегаты | Выполнено: TestDataGenerator, Practice, тесты |
| P04 | Equals/GetHashCode, новый равный ключ, несколько счетов, список сотрудников | Выполнено: EquivalenceTests и RegressionTests |
| P05 | Коллекции и min/max/average возраста обоих типов | Выполнено: Storages и LINQ-тесты |
| P06 | Клиент/сотрудник, исключения, паспорт/18+, USD, счета, фильтр | Выполнено: сервисы и граничные тесты |
| P07 | IStorage<T>, интерфейсы клиентов/сотрудников, зависимости | Выполнено: Abstractions.cs, memory/EF storage |
| P08 | PostgreSQL, таблицы, PK/FK, один-ко-многим | Выполнено: SQL и изолированный PostgreSQL verifier |
| P09 | INSERT, low/min/sum/JOIN/возраст/group/limit | Выполнено: sql/banksystem.sql, реальный PostgreSQL |
| P10 | EF/config/migrations/CRUD/счета/pagination/SQL/debugger | Выполнено: Data.Tests; Rider остановлен перед Count, затем Count=12 и ToList=5; собственный breakpoint удалён |
| P11 | CSV export/import в БД, Stream/using | Выполнено: ExportTool, CSV-тесты, SQLite и PostgreSQL |
| P12 | Конвейер/ротация/чтение; два Thread ×10×100 | Выполнено: ClientFilePipeline и ThreadAndTaskTests |
| P13 | Месячный фон, N списаний, async storage/service | Выполнено: RateUpdaterService, WithdrawManyAsync, Data.Tests; расчёт месяца/отмена/сохранение начислений проверены отдельно |
| P14 | Обобщённый JSON Client/Employee | Выполнено: ExportService и round-trip тесты |
| P15 | API клиентов/сотрудников, DTO, mapping, validation, DI, Swagger/debug | Выполнено: API integration tests; Swagger/browser и путь HTTP в Rider ранее проверены, обработчики актуального API проверяются тестами |
| P16 | CurrencyService и настоящий внешний HTTP | Выполнено: Frankfurter, HTTP-тесты и успешный live currency-smoke; DEX не входит в практику |
| P17 | Postman: cURL/OpenAPI import, environment, цепочка, tests/logs | Частично: настоящая коллекция Newman 12/14 проходит; импорт cURL из DevTools и OpenAPI в Postman GUI не подтверждены |
| P18 | Проект тестов и повтор практики эквивалентности | Выполнено: App.Tests; отсылка к P04 |
| S01 | ООП + консоль | Выполнено: Animal/Dog/Cat/IAnimal и Practice |
| S02 | Stopwatch boxing/unboxing | Выполнено: отдельные циклы Practice |
| S03 | INotifyPropertyChanged | Выполнено: ObservableBalance, Practice, тест |
| S04 | События очереди | Выполнено: ThresholdQueue, Practice, тест |
| S05 | Процентное отличие потока чисел | Выполнено: NumberStreamAnalyzer и тесты |
| S06 | Bogus | Выполнено: случайные клиенты/сотрудники + тест |
| S07 | 10 фигур, CompareTo, убывание площади | Выполнено: Circle и Practice |
| S08 | IComparer | Выполнено: CircleAreaDescendingComparer и тест |
| S09 | int→TimeSpan extension | Выполнено: Seconds и тест |
| S10 | Reflection: создание по имени | Выполнено: Create(string) и тест |
| S11 | Reflection: вызов с параметрами | Выполнено: Invoke и тест |
| S12 | Reflection: закрытые свойства | Выполнено: GetPrivateProperty и тест |
| S13 | Вложенный JSON | Выполнено: восстановление счетов/баланса/валюты |
| S14 | Отдельный проект калькулятора, 4 операции | Выполнено: ConsoleApp4 + тесты; исторический порядок TDD не утверждается |
| O01 | Bonus/blacklist с generics | Выполнено дополнительное задание: BankService |
| O02 | Currency entity и миграция | Выполнено задание со звёздочкой: текущая схема и миграции |

Итог: **31/32 основных пунктов полностью проверен, P17 частично; 2/2 дополнительных выполнены**. Кода для незавершённых обязательных методов не осталось. Для формального завершения P17 нужен Postman GUI, а не учётная запись DEX. Теоретические вопросы и внешний Git-процесс преподавателя учитываются отдельно и не объявляются усвоенными/выполненными по тестам.

Текущие тесты: 50 App.Tests + 12 Data.Tests. Удаление 7 тестов примеров, объединение 1 дубликата и замена 1 сложного календарного сценария 4 простыми объясняют изменение общего числа с 67 на 62; проверок обязательной функциональности ради зелёного результата не отключалось.
