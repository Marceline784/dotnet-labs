# Лаб. 06: gRPC і кешування

## Що додано

| Місце | Що |
|---|---|
| `Platform.AppHost` | контейнер Redis (`AddRedis("redis").WithDataVolume()`), посилання й `WaitFor` для wardrobe-api, feedback-api, aggregator |
| `Wardrobe.Bll/Caching` | `TwoLevelCache` (L1 `IMemoryCache` з `SizeLimit`, L2 Redis), cache-aside, версії ключів для списків, метрики `Wearcast.Wardrobe.Cache`; декоратори `CachedWardrobeItemService`, `CachedCategoryService` |
| `Feedback.Application/Caching` | той самий `TwoLevelCache`; `CachingBehavior` у pipeline MediatR (`ICacheableQuery`, `ICacheInvalidatingCommand`) |
| `*/Protos/*.proto` | контракти `wardrobe.proto`, `recommendations.proto`, `feedback.proto` |
| `*/Grpc/` | gRPC-сервіси як адаптери над BLL, `RpcException` (`NotFound`, `InvalidArgument`), `LoggingInterceptor` |
| `Platform.Aggregator` | gRPC-клієнти з service discovery, паралельні виклики `Task.WhenAll`, кеш відповіді в Redis (TTL 30 с), метрики `Wearcast.Aggregator.Cache` |

## Кешування

- Ключі: `wardrobe:item:e{epoch}:{id}`, `wardrobe:list:v{version}:{hash}`, `feedback:data:v{version}:{запит}`, `aggregator:recommendation:{id}:full`.
- L1: sliding 30 с, absolute 1 хв. L2: absolute 10 хв. L1 коротший за L2.
- Інвалідація виконується синхронно з записом: ключ картки видаляється, версія списків збільшується.
- Redis недоступний: сервіс працює через L1 і БД, Redis пропускається на 15 с.
- Часткова відповідь Aggregator не кешується.

## Порівняння HTTP (Лаб. 05) і gRPC

Запит `GET /api/aggregator/recommendations/1/full`, тривалість span `gateway` у Traces.

| Варіант | Без кешу, мс | З кешем Redis, мс |
|---|---|---|
| HTTP (гілка lab-05) | | |
| gRPC (Лаб. 06) | | |
