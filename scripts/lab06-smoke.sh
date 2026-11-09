#!/usr/bin/env bash
# Лаб. 06: перевірка кешу й gRPC-агрегації через Gateway (AppHost має працювати). Запуск: bash scripts/lab06-smoke.sh
BASE=http://localhost:5000
h() { echo; echo "=== $*"; }

h "Wardrobe: картка речі 1 (двічі: другий запит із кешу, без SQL-span)"
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" "$BASE/api/wardrobe-items/1"
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" "$BASE/api/wardrobe-items/1"

h "Wardrobe: категорії (довідник у кеші)"
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" "$BASE/api/categories"
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" "$BASE/api/categories"

h "Aggregator через gRPC: повна картка (1-й запит: gRPC, 2-й: Redis)"
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" "$BASE/api/aggregator/recommendations/1/full"
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" "$BASE/api/aggregator/recommendations/1/full"

h "Aggregator: рекомендації, якої немає (404)"
curl -s -i "$BASE/api/aggregator/recommendations/999999/full" | head -n 8

h "Інвалідація: PUT речі 1, потім GET (має повернути оновлені дані)"
echo "Зробіть PUT /api/wardrobe-items/1 зі Swagger wardrobe-api і повторіть GET вище."
