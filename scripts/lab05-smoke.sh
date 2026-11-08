#!/usr/bin/env bash
# Лаб. 05: перевірка через Gateway (AppHost має працювати). Запуск: bash scripts/lab05-smoke.sh
BASE=http://localhost:5000
h() { echo; echo "=== $*"; }

h "recommendations-api через Gateway: користувачі"
curl -s -i "$BASE/api/users" | head -n 12

h "wardrobe-api через Gateway: речі"
curl -s -i "$BASE/api/wardrobe-items?page=1&pageSize=3" | head -n 12

h "feedback-api через Gateway: відгуки"
curl -s -i "$BASE/api/feedback?pageSize=3" | head -n 12

h "створити рекомендацію (POST) для користувача 1"
curl -s -i -X POST "$BASE/api/recommendations" -H "Content-Type: application/json" \
  -d '{"userId":1,"outfitCount":3}' | head -n 14

h "Aggregator: повна картка рекомендації 1"
curl -s -i "$BASE/api/aggregator/recommendations/1/full" | head -n 30

h "Aggregator: рекомендації, якої немає (очікується 404 ProblemDetails)"
curl -s -i "$BASE/api/aggregator/recommendations/999999/full" | head -n 12

h "Власний CorrelationId клієнта зберігається"
curl -s -i -H "X-Correlation-Id: demo-123" "$BASE/api/categories" | grep -i "x-correlation-id"
