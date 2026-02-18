# 🚨 ВАЖНО: ИСПРАВЛЕНИЕ ENDPOINTS ДЛЯ ФРОНТЕНДА

## ❌ ПРОБЛЕМА: НЕСООТВЕТСТВИЕ ENDPOINTS

Фронтенд использует неправильные endpoints. Вот правильная структура:

---

## ✅ ПРАВИЛЬНЫЕ ENDPOINTS В C# BACKEND

### 1. Dashboard Метрики (для карточек серверов)
**Правильный URL:** `GET /api/servers/{serverId}/metrics/dashboard`

**Пример:**
```bash
GET /api/servers/srv_7e2e571c/metrics/dashboard
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "serverId": "srv_7e2e571c",
  "timeRange": {
    "start": "2026-02-15T17:00:00Z",
    "end": "2026-02-15T18:00:00Z"
  },
  "granularity": "minute",
  "dataPoints": [...],
  "totalPoints": 60
}
```

---

### 2. Historical Метрики (для графиков)
**Правильный URL:** `GET /api/servers/{serverId}/metrics/tiered`

**Параметры:**
- `start` - ISO 8601 datetime (обязательно)
- `end` - ISO 8601 datetime (обязательно)
- `granularity` - string (опционально)

**Пример:**
```bash
GET /api/servers/srv_7e2e571c/metrics/tiered?start=2026-02-15T15:00:00Z&end=2026-02-15T16:00:00Z&granularity=1h
Authorization: Bearer {jwt_token}
```

**Поддерживаемые значения granularity:**
- `"minute"` - 1 минута
- `"hour"` - 1 час
- `"day"` - 1 день
- `"week"` - 1 неделя
- `"month"` - 1 месяц

---

### 3. Realtime Метрики (для live данных)
**Правильный URL:** `GET /api/servers/{serverId}/metrics/realtime`

**Параметры:**
- `duration` - string (опционально)

**Пример:**
```bash
GET /api/servers/srv_7e2e571c/metrics/realtime?duration=30m
Authorization: Bearer {jwt_token}
```

**Поддерживаемые значения duration:**
- `"5m"` - 5 минут
- `"15m"` - 15 минут
- `"30m"` - 30 минут
- `"1h"` - 1 час
- `"24h"` - 24 часа

---

### 4. WebSocket Token (для live streaming)
**Правильный URL:** `POST /api/servers/{serverId}/metrics/live-token`

**Пример:**
```bash
POST /api/servers/srv_7e2e571c/metrics/live-token
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "wsUrl": "ws://localhost:8080/ws?token=...",
  "expiresAt": "2026-02-15T18:32:54Z"
}
```

---

## ❌ НЕПРАВИЛЬНЫЕ ENDPOINTS (не существуют)

Эти endpoints НЕ существуют в C# backend:

```bash
# ❌ НЕ СУЩЕСТВУЕТ
GET /api/servers/{serverId}/metrics/historical

# ❌ НЕ СУЩЕСТВУЕТ  
GET /api/servers/{serverId}/metrics/dashboard?start=...&end=...
```

---

## 🎯 ИСПРАВЛЕНИЯ ДЛЯ ФРОНТЕНДА

### Заменить в коде:

**Было:**
```javascript
// ❌ Неправильно
fetch(`/api/servers/${serverId}/metrics/historical?start=${start}&end=${end}&granularity=${granularity}`)
fetch(`/api/servers/${serverId}/metrics/dashboard`)
```

**Стало:**
```javascript
// ✅ Правильно
fetch(`/api/servers/${serverId}/metrics/tiered?start=${start}&end=${end}&granularity=${granularity}`)
fetch(`/api/servers/${serverId}/metrics/dashboard`)
```

---

## 📋 ПОЛНЫЙ СПИСОК РАБОЧИХ ENDPOINTS

### Аутентификация:
- `POST /api/users/register`
- `POST /api/users/login`
- `POST /api/auth/refresh`
- `GET /api/users/me`

### Управление серверами:
- `GET /api/monitoredservers`
- `POST /api/monitoredservers/add`
- `POST /api/monitoredservers/{serverId}/share`
- `DELETE /api/monitoredservers/{serverId}`

### Метрики:
- `GET /api/servers/{serverId}/metrics/dashboard` ✅
- `GET /api/servers/{serverId}/metrics/tiered?start={start}&end={end}&granularity={granularity}` ✅
- `GET /api/servers/{serverId}/metrics/realtime?duration={duration}` ✅
- `POST /api/servers/{serverId}/metrics/live-token` ✅

---

## 🧪 РАБОЧИЕ ПРИМЕРЫ

### Dashboard (текущие значения):
```bash
GET /api/servers/srv_7e2e571c/metrics/dashboard
```

### Historical (графики за период):
```bash
GET /api/servers/srv_7e2e571c/metrics/tiered?start=2026-02-15T15:00:00Z&end=2026-02-15T16:00:00Z&granularity=1h
```

### Realtime (последние 30 минут):
```bash
GET /api/servers/srv_7e2e571c/metrics/realtime?duration=30m
```

### WebSocket Token:
```bash
POST /api/servers/srv_7e2e571c/metrics/live-token
```

---

## 🚀 ГОТОВО К ИНТЕГРАЦИИ

Все endpoints работают и протестированы! Используйте правильные URL и параметры.

**Главное отличие:**
- ❌ `/metrics/historical` → ✅ `/metrics/tiered`
- ✅ `/metrics/dashboard` (без параметров)
- ✅ `/metrics/realtime?duration=Xm/Xh`

**Фронтенд разработчики могут начинать интеграцию с правильными endpoints!** 🎯
