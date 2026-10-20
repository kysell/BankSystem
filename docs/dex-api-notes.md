# Проверка внешнего учебного DEX API

Проверено 2026-10-20 без учётных данных:

- `http://dev.trainee.dex-it.ru/swagger/index.html` — 200.
- `http://dev.trainee.dex-it.ru/swagger/v1/swagger.json` — 200, BasketBallClubSample v1.
- `http://dev.trainee.dex-it.ru/api/Team/GetTeams` — 401 без Bearer.
- `https://dev.trainee.dex-it.ru/swagger/index.html` — curl отказался устанавливать соединение: self-signed certificate. Проверка сертификата не отключалась.

По публичной схеме `TeamDto.foundationYear` имеет тип integer/int32. Это исправлено в нашем Team. Тела SignIn/Add/GetTeams, Bearer, отказ авторизации и сброс старого токена проверены с fake HttpMessageHandler. Регистрация, вход с выдуманными данными и запись команд на внешний сервер не выполнялись.

Для полноценной live-проверки нужен актуальный **HTTPS endpoint с корректным сертификатом** и собственная учебная учётная запись. Это внешняя зависимость; ни установка Newman, ни повторение локальных тестов не заменят авторизацию на сервере. TeamService запрещает HTTP, чтобы случайно не отправить пароль или токен без TLS.

Безопасная конфигурация: `TeamApi__BaseUrl` в Environment variables конфигурации BankSystem.Api в Rider. Логин/пароль не находятся в appsettings, Postman-коллекции или исходниках. Передавайте AuthData в собственном локальном вызове, не записывая секреты в отчёт и не отправляя их в чат.

До получения безопасного адреса достаточно демонстрировать `HttpContractTests` и локальный BankSystem API. По №23 реальный CurrencyService проверен, а авторизованные DEX вызовы остаются непроверенными.
