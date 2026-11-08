# Лаб. 05: платформа на .NET Aspire (Gateway, Aggregator, Serilog, OpenTelemetry)

## Склад

| Проєкт | Роль |
|---|---|
| `Platform.AppHost` | оркестрація: SQL Server + MongoDB (з volume), три сервіси, Aggregator, Gateway |
| `Platform.ServiceDefaults` | Serilog (JSON + OTLP), OpenTelemetry (ASP.NET Core, HttpClient, SqlClient, MongoDB), CorrelationId, health checks, service discovery, resilience |
| `Platform.ApiGateway` | YARP, порт **5000**, маршрути й кластери в `appsettings.json`, лог рішень маршрутизації, метрики `gateway.*` |
| `Platform.Aggregator` | `GET /api/aggregator/recommendations/{id}/full`: рекомендація + речі гардероба + відгуки |

Сервіси з Лаб. 02–04 змінені лише в `Program.cs` (`AddServiceDefaults()`, `MapDefaultEndpoints()`; у Wardrobe та Feedback прибрано власний `UseSerilog`) і в `.csproj` (reference на ServiceDefaults). Бізнес-логіка не чіпалась.

## Запуск

```bash
dotnet user-secrets set "Parameters:sql-password" "Wearcast_Pass123!" --project Platform.AppHost    # одноразово
dotnet user-secrets set "Parameters:mongo-password" "WearcastMongo123" --project Platform.AppHost   # одноразово
dotnet run --project Platform.AppHost       # Dashboard відкриється сам
bash scripts/lab05-smoke.sh                 # запити через Gateway
```

Паролі контейнерів лежать у user-secrets AppHost (`Parameters:sql-password`, `Parameters:mongo-password`). Дані SQL Server і MongoDB зберігаються у Docker volume між перезапусками.

Для демо без інтернету: `Weather__Provider=Fake` (змінна оточення для `recommendations-api`).

## Маршрути Gateway (http://localhost:5000)

| Шлях | Сервіс |
|---|---|
| `/api/aggregator/**` | aggregator |
| `/api/users/{id}/laundry` | wardrobe-api (Order 0, специфічніший за `/api/users/**`) |
| `/api/recommendations/**`, `/api/outfits/**`, `/api/users/**` | recommendations-api |
| `/api/wardrobe-items/**`, `/api/categories/**` | wardrobe-api |
| `/api/feedback/**` | feedback-api |

Збіг шляхів: `/api/wardrobe-items/{id}` існує і в Recommendations (власна SQL-проєкція речей), і в Wardrobe. Через Gateway цей шлях іде в **wardrobe-api** (власник даних); копія в Recommendations доступна лише напряму.

## Що дивитись у Dashboard

1. **Resources**: `sql`, `mongo`, `recommendations-api`, `wardrobe-api`, `feedback-api`, `aggregator`, `gateway` у статусі Running.
2. **Traces**: запит `/api/aggregator/recommendations/1/full`: `gateway` → `aggregator` → `recommendations-api` → SQL; далі паралельно `wardrobe-api` (SQL) і `feedback-api` (MongoDB).
3. **Structured logs**: фільтр `CorrelationId = <значення із заголовка відповіді>`; у записах `ServiceName`, `Environment`, `MachineName`, `ThreadId`, `TraceId`, `SpanId`.
4. **Metrics**: `gateway.requests`, `gateway.request.duration`.

## Сценарій відмови

1. Dashboard → зупинити `feedback-api` (або `wardrobe-api`).
2. `curl -i http://localhost:5000/api/aggregator/recommendations/1/full` → **200**, у `warnings` є `feedback-api is unavailable`, решта даних на місці.
3. Запустити сервіс знову (Start у Dashboard); circuit breaker закривається за ~10 с, після чого відповідь знову повна.
4. Зупинка `recommendations-api` дає **503** ProblemDetails (без рекомендації агрегувати нічого).
5. Прямий виклик сервісу в обхід Gateway (адреса в Dashboard): CorrelationId створює сам сервіс (ServiceDefaults), але в логах немає рядка маршрутизації Gateway (`route`, `cluster`, `destination`).
