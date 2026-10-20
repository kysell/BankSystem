# BankSystem — восстановление после ревизии №121

Рабочая ветка revision/dex121-requirements. Начальный commit 73fb959 сохранён; публикация в удалённый репозиторий не выполнялась. Исходные 6 незакоммиченных изменений до ревизии сохранены в локальном исключённом из Git файле artifacts/verification/pre-requirements-audit.patch. Удаляемые авторские примеры доступны в исходном commit и этом patch.

## Источник и объём

Найден настоящий PDF /Users/admin/Downloads/Конспект_курса_DEX_по_Backend.pdf, 143 стр., версия №121; три копии идентичны. SHA-256 и цитаты — requirements-audit.md. PDF не добавлен в репозиторий.

Счёт: 18 самостоятельных блоков практики + 14 явных самостоятельных заданий с кодом = 32 основных пункта; 2 дополнительных блока. 31 полностью проверен, P17 частично (GUI import cURL/OpenAPI). DEX live не является требованием. Старые 25/26 не применять.

## Завершённые группы

1. Удалены TeamService и его модели/DI/5 тестов; MemoryWorker/SafeHandle/2 теста; WordCount, PrintObjectProperties, неиспользуемый ref Currency overload. Два обычных validation tests сохранены в BoundaryTests. Группа прошла 60/60.
2. Калькулятор перенесён в ConsoleApp4, вместо Hello World запускаются 4 операции. Добавлен требуемый на стр. 113 Data.Tests; туда перенесены 8 EF-тестов и простые проверки RateUpdaterService. SQLite пакеты перенесены из App.Tests. Месячный цикл: обычный Task.Delay + расчёт границы месяца; самодельные часы/таймер/storage-декоратор удалены.
3. Богус-конфигурация развёрнута в последовательные RuleFor; контроллеры — небольшие методы с явными переменными; дубли DataAnnotations убраны, FluentValidation сохранён. Одна дублирующая проверка пополнения объединена с настоящим Thread-тестом. Сборка без ошибок/предупреждений; тесты 50 App +12 Data =62.
4. Удалены собственный Postman runtime и Playwright script/dependency; Newman и коллекции сохранены. PostgreSQL verifier больше не зависит от самописного runner.
5. Настоящая отладка EF в Rider: testhost 11791, breakpoint ClientDbStorage.Search перед Count; query EntityQueryable, после Count=12, после ToList=5. Тест завершён успешно, свой breakpoint удалён, debugger sessions=[].
6. README и все учебные документы переписаны по фактическим пунктам PDF. Прежний DEX-отчёт удалён. CurrencyService возвращает decimal без лишнего DTO; неиспользуемый EmployeeService.SearchAsync удалён. ThreadAndTaskTests и SimpleCalculatorTests выделены в понятные отдельные файлы.

## Проверки и следующий шаг

Финальные restore/build успешны, 0 warnings/errors; 62/62 тестов (50 App +12 Data), пропусков нет. Логи revision-restore/build/tests.log в artifacts/verification; TRX в artifacts/test-results/revision. Practice и ConsoleApp4 реально запущены и завершились успешно. После упрощения CurrencyService реальный запрос дал 10 USD → 8.91 EUR.

Повторный PostgreSQL verifier прошёл полностью: миграции/SQL/CRUD/CSV/API, Newman 12 запросов/14 assertions, ещё 3/3 HTTP NUnit. Кластер /tmp/banksystem-training.Fz3r2A остановлен; лог artifacts/verification/revision-postgres.log. Рабочие базы не использовались. Для solution оставлены исходные Debug/Release Any CPU; автоматически добавленные dotnet sln платформы x86/x64 удалены. Всего 9 проектов.

Следующий шаг при продолжении: прочитать этот файл и requirements-audit.md, проверить git status. Ревизия кода завершена; обязательный неподтверждённый шаг — процедура GUI Postman. Не пытаться «закрыть» её Newman или DEX account. Не выполнять новые commit или push без разрешения.
