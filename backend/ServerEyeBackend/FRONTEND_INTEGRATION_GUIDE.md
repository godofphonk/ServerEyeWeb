# 🚀 ServerEye Frontend Integration Guide

## 📌 Архитектура

```
┌─────────────┐      HTTP/REST      ┌──────────────┐      HTTP      ┌─────────────┐
│   Frontend  │ ──────────────────> │  C# Backend  │ ────────────> │   Go API    │
│  (React/JS) │ <────────────────── │ (ServerEye)  │ <──────────── │  (Metrics)  │
└─────────────┘    JSON + JWT       └──────────────┘    JSON        └─────────────┘
                                            │
                                            │ PostgreSQL
                                            ▼
                                     ┌──────────────┐
                                     │   Database   │
                                     └──────────────┘
```

**ВАЖНО:** Фронтенд общается **ТОЛЬКО** с C# Backend. Прямые запросы к Go API запрещены!

---

## 🔧 Base Configuration

### Development URLs
```typescript
const API_BASE_URL = 'http://localhost:5246';
const WS_BASE_URL = 'ws://localhost:8080'; // только для WebSocket после получения токена
```

### Production URLs
```typescript
const API_BASE_URL = 'https://api.servereye.com';
const WS_BASE_URL = 'wss://metrics.servereye.com';
```

---

## 🔐 1. AUTHENTICATION

### 1.1 User Registration

**Endpoint:** `POST /api/users/register`

**Request:**
```typescript
interface RegisterRequest {
  userName: string;    // минимум 3 символа
  email: string;       // валидный email
  password: string;    // минимум 8 символов, должен содержать цифры и спецсимволы
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/users/register`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    userName: 'john_doe',
    email: 'john@example.com',
    password: 'SecurePass123!@#'
  })
});

const data = await response.json();
```

**Response (200 OK):**
```typescript
interface RegisterResponse {
  token: string;        // JWT токен для авторизации
  refreshToken: string; // Refresh token для обновления
  expiresIn: number;    // Время жизни токена в секундах (3600)
}
```

**Errors:**
- `400` - Validation failed (некорректные данные)
- `409` - User already exists (email уже зарегистрирован)
- `500` - Internal server error

---

### 1.2 User Login

**Endpoint:** `POST /api/users/login`

**Request:**
```typescript
interface LoginRequest {
  email: string;
  password: string;
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/users/login`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'john@example.com',
    password: 'SecurePass123!@#'
  })
});

const data = await response.json();
```

**Response (200 OK):**
```typescript
interface LoginResponse {
  token: string;        // JWT токен
  refreshToken: string; // Refresh token
  expiresIn: number;    // 3600 секунд (1 час)
}
```

**Errors:**
- `400` - Invalid credentials
- `401` - Unauthorized
- `500` - Internal server error

---

### 1.3 Get Current User

**Endpoint:** `GET /api/users/me`

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/users/me`, {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
  }
});

const user = await response.json();
```

**Response (200 OK):**
```typescript
interface UserResponse {
  id: string;           // UUID
  userName: string;
  email: string;
  createdAt: string;    // ISO 8601
}
```

---

### 1.4 Token Management

**Сохранение токена:**
```typescript
// После успешного login/register
localStorage.setItem('jwt_token', data.token);
localStorage.setItem('refresh_token', data.refreshToken);
localStorage.setItem('token_expires_at', Date.now() + data.expiresIn * 1000);
```

**Проверка валидности:**
```typescript
function isTokenValid(): boolean {
  const expiresAt = localStorage.getItem('token_expires_at');
  return expiresAt ? Date.now() < parseInt(expiresAt) : false;
}
```

**Использование в запросах:**
```typescript
const token = localStorage.getItem('jwt_token');

const response = await fetch(url, {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  }
});
```

---

## 🖥️ 2. SERVER MANAGEMENT

### 2.1 Get User Servers

**Endpoint:** `GET /api/monitoredservers`

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/monitoredservers`, {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
  }
});

const servers = await response.json();
```

**Response (200 OK):**
```typescript
interface ServerResponse {
  id: string;              // UUID (internal DB ID)
  serverId: string;        // srv_* (Go API server ID)
  hostname: string;        // Server hostname
  operatingSystem: string; // "Linux", "Windows", etc.
  accessLevel: number;     // 1=Viewer, 2=Admin, 3=Owner
  addedAt: string;         // ISO 8601
  lastSeen: string;        // ISO 8601
  isActive: boolean;       // Server online status
}

type ServersListResponse = ServerResponse[];
```

**Example Response:**
```json
[
  {
    "id": "722ee628-e596-40cd-87ca-928148bbafc0",
    "serverId": "srv_a3d881f1",
    "hostname": "web-server-01",
    "operatingSystem": "Linux",
    "accessLevel": 3,
    "addedAt": "2026-02-15T17:46:07.829451Z",
    "lastSeen": "2026-02-15T18:14:07.57189Z",
    "isActive": true
  }
]
```

---

### 2.2 Add Server

**Endpoint:** `POST /api/monitoredservers/add`

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}',
  'Content-Type': 'application/json'
}
```

**Request:**
```typescript
interface AddServerRequest {
  serverKey: string; // key_* формат (получается из агента)
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/monitoredservers/add`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    serverKey: 'key_54b8ab51'
  })
});

const server = await response.json();
```

**Response (200 OK):**
```typescript
interface ServerResponse {
  id: string;
  serverId: string;
  hostname: string;
  operatingSystem: string;
  accessLevel: number;
  addedAt: string;
  lastSeen: string;
  isActive: boolean;
}
```

**Errors:**
- `400` - Invalid server key
- `403` - Forbidden (no access)
- `409` - Server already added
- `500` - Internal server error

---

### 2.3 Remove Server

**Endpoint:** `DELETE /api/monitoredservers/{serverId}`

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/monitoredservers/srv_a3d881f1`, {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${token}`,
  }
});

// Response: 204 No Content
```

**Errors:**
- `403` - Forbidden (only owner can remove)
- `404` - Server not found
- `500` - Internal server error

---

### 2.4 Share Server

**Endpoint:** `POST /api/monitoredservers/{serverId}/share`

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}',
  'Content-Type': 'application/json'
}
```

**Request:**
```typescript
interface ShareServerRequest {
  targetUserEmail: string;
  accessLevel: number; // 1=Viewer, 2=Admin
}
```

**Example:**
```typescript
const response = await fetch(`${API_BASE_URL}/api/monitoredservers/srv_a3d881f1/share`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    targetUserEmail: 'colleague@example.com',
    accessLevel: 1
  })
});

const result = await response.json();
```

**Response (200 OK):**
```json
{
  "message": "Server shared successfully"
}
```

**Errors:**
- `403` - Forbidden (only owner can share)
- `404` - User not found
- `500` - Internal server error

---

## 📊 3. METRICS

### 3.1 Dashboard Metrics (Current State)

**Endpoint:** `GET /api/servers/{serverId}/metrics/dashboard`

**Назначение:** Получить текущие метрики сервера для отображения на dashboard (последние 5 минут)

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Example:**
```typescript
const response = await fetch(
  `${API_BASE_URL}/api/servers/srv_a3d881f1/metrics/dashboard`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
    }
  }
);

const metrics = await response.json();
```

**Response (200 OK):**
```typescript
interface MetricsResponse {
  serverId: string;
  serverName: string;
  startTime: string;      // ISO 8601
  endTime: string;        // ISO 8601
  granularity: string;    // "1m", "5m", "1h", etc.
  dataPoints: DataPoint[];
  totalPoints: number;
}

interface DataPoint {
  timestamp: string;      // ISO 8601
  cpu: MetricValue;
  memory: MetricValue;
  disk: MetricValue;
  network: MetricValue;
}

interface MetricValue {
  avg: number;
  max: number;
  min: number;
}
```

**Example Response:**
```json
{
  "serverId": "srv_a3d881f1",
  "serverName": "web-server-01",
  "startTime": "2026-02-15T18:24:41Z",
  "endTime": "2026-02-15T18:29:41Z",
  "granularity": "1m",
  "dataPoints": [
    {
      "timestamp": "2026-02-15T18:25:00Z",
      "cpu": { "avg": 17.71, "max": 17.72, "min": 17.70 },
      "memory": { "avg": 70.64, "max": 71.56, "min": 70.08 },
      "disk": { "avg": 66, "max": 66, "min": 66 },
      "network": { "avg": 0.3, "max": 1.2, "min": 0.1 }
    }
  ],
  "totalPoints": 5
}
```

**Use Case:**
```typescript
// Отображение текущего состояния сервера на карточке
function ServerCard({ serverId }: { serverId: string }) {
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      const response = await fetch(
        `${API_BASE_URL}/api/servers/${serverId}/metrics/dashboard`,
        {
          headers: { 'Authorization': `Bearer ${token}` }
        }
      );
      const data = await response.json();
      setMetrics(data);
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 60000); // обновление каждую минуту
    return () => clearInterval(interval);
  }, [serverId]);

  if (!metrics) return <div>Loading...</div>;

  const latest = metrics.dataPoints[metrics.dataPoints.length - 1];
  
  return (
    <div className="server-card">
      <h3>{metrics.serverName}</h3>
      <div>CPU: {latest.cpu.avg.toFixed(1)}%</div>
      <div>Memory: {latest.memory.avg.toFixed(1)}%</div>
      <div>Disk: {latest.disk.avg.toFixed(0)}%</div>
    </div>
  );
}
```

---

### 3.2 Realtime Metrics (Recent Data)

**Endpoint:** `GET /api/servers/{serverId}/metrics/realtime?duration={duration}`

**Назначение:** Получить метрики за последние N минут/часов для графиков в реальном времени

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Query Parameters:**
```typescript
interface RealtimeParams {
  duration?: string; // "5m", "15m", "30m", "1h", "24h" (default: "5m")
}
```

**Example:**
```typescript
const response = await fetch(
  `${API_BASE_URL}/api/servers/srv_a3d881f1/metrics/realtime?duration=30m`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
    }
  }
);

const metrics = await response.json();
```

**Response:** Same as Dashboard (MetricsResponse)

**Use Case:**
```typescript
// График метрик в реальном времени
function RealtimeChart({ serverId }: { serverId: string }) {
  const [duration, setDuration] = useState('30m');
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      const response = await fetch(
        `${API_BASE_URL}/api/servers/${serverId}/metrics/realtime?duration=${duration}`,
        {
          headers: { 'Authorization': `Bearer ${token}` }
        }
      );
      const data = await response.json();
      setMetrics(data);
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 30000); // обновление каждые 30 секунд
    return () => clearInterval(interval);
  }, [serverId, duration]);

  return (
    <div>
      <select value={duration} onChange={(e) => setDuration(e.target.value)}>
        <option value="5m">Last 5 minutes</option>
        <option value="15m">Last 15 minutes</option>
        <option value="30m">Last 30 minutes</option>
        <option value="1h">Last 1 hour</option>
      </select>
      <LineChart data={metrics?.dataPoints} />
    </div>
  );
}
```

---

### 3.3 Tiered Metrics (Historical Data)

**Endpoint:** `GET /api/servers/{serverId}/metrics/tiered?start={start}&end={end}&granularity={granularity}`

**Назначение:** Получить исторические метрики за произвольный период с указанной детализацией

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Query Parameters:**
```typescript
interface TieredParams {
  start: string;        // ISO 8601 (required)
  end: string;          // ISO 8601 (required)
  granularity: string;  // "minute", "hour", "day", "week", "month" (required)
}
```

**Example:**
```typescript
const startDate = new Date('2026-02-15T10:00:00Z').toISOString();
const endDate = new Date('2026-02-15T18:00:00Z').toISOString();

const response = await fetch(
  `${API_BASE_URL}/api/servers/srv_a3d881f1/metrics/tiered?` +
  `start=${encodeURIComponent(startDate)}&` +
  `end=${encodeURIComponent(endDate)}&` +
  `granularity=hour`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
    }
  }
);

const metrics = await response.json();
```

**Response:** Same as Dashboard (MetricsResponse)

**Granularity Options:**
- `minute` - для периодов до 1 часа
- `hour` - для периодов до 1 дня
- `day` - для периодов до 1 месяца
- `week` - для периодов до 6 месяцев
- `month` - для периодов больше 6 месяцев

**Use Case:**
```typescript
// Исторический график с выбором периода
function HistoricalChart({ serverId }: { serverId: string }) {
  const [period, setPeriod] = useState('24h');
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      const end = new Date();
      let start = new Date();
      let granularity = 'hour';

      switch (period) {
        case '24h':
          start.setHours(start.getHours() - 24);
          granularity = 'hour';
          break;
        case '7d':
          start.setDate(start.getDate() - 7);
          granularity = 'day';
          break;
        case '30d':
          start.setDate(start.getDate() - 30);
          granularity = 'day';
          break;
      }

      const response = await fetch(
        `${API_BASE_URL}/api/servers/${serverId}/metrics/tiered?` +
        `start=${start.toISOString()}&` +
        `end=${end.toISOString()}&` +
        `granularity=${granularity}`,
        {
          headers: { 'Authorization': `Bearer ${token}` }
        }
      );
      const data = await response.json();
      setMetrics(data);
    };

    fetchMetrics();
  }, [serverId, period]);

  return (
    <div>
      <select value={period} onChange={(e) => setPeriod(e.target.value)}>
        <option value="24h">Last 24 hours</option>
        <option value="7d">Last 7 days</option>
        <option value="30d">Last 30 days</option>
      </select>
      <LineChart data={metrics?.dataPoints} />
    </div>
  );
}
```

---

## 🔴 4. WEBSOCKET (Live Streaming)

### 4.1 Generate WebSocket Token

**Endpoint:** `POST /api/servers/{serverId}/metrics/live-token`

**Назначение:** Получить JWT токен для подключения к WebSocket для получения метрик в реальном времени

**Headers:**
```typescript
{
  'Authorization': 'Bearer {jwt_token}'
}
```

**Example:**
```typescript
const response = await fetch(
  `${API_BASE_URL}/api/servers/srv_a3d881f1/metrics/live-token`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
    }
  }
);

const wsData = await response.json();
```

**Response (200 OK):**
```typescript
interface WebSocketTokenResponse {
  token: string;      // JWT токен для WebSocket
  wsUrl: string;      // Полный URL для подключения
  expiresAt: string;  // ISO 8601 (токен действует 30 минут)
}
```

**Example Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "wsUrl": "ws://localhost:8080/ws?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-15T18:59:56Z"
}
```

---

### 4.2 WebSocket Connection

**Example:**
```typescript
class MetricsWebSocket {
  private ws: WebSocket | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;

  async connect(serverId: string, onMessage: (data: any) => void) {
    // 1. Получаем WebSocket токен
    const response = await fetch(
      `${API_BASE_URL}/api/servers/${serverId}/metrics/live-token`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`,
        }
      }
    );

    const { wsUrl, expiresAt } = await response.json();

    // 2. Подключаемся к WebSocket
    this.ws = new WebSocket(wsUrl);

    this.ws.onopen = () => {
      console.log('WebSocket connected');
      this.reconnectAttempts = 0;
    };

    this.ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      onMessage(data);
    };

    this.ws.onerror = (error) => {
      console.error('WebSocket error:', error);
    };

    this.ws.onclose = () => {
      console.log('WebSocket closed');
      
      // Автоматическое переподключение
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        this.reconnectAttempts++;
        setTimeout(() => this.connect(serverId, onMessage), 5000);
      }
    };

    // 3. Переподключение перед истечением токена
    const expiresIn = new Date(expiresAt).getTime() - Date.now();
    setTimeout(() => {
      this.disconnect();
      this.connect(serverId, onMessage);
    }, expiresIn - 60000); // переподключаемся за минуту до истечения
  }

  disconnect() {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }
}
```

**WebSocket Message Format:**
```typescript
interface WebSocketMessage {
  type: 'metrics' | 'status' | 'error';
  serverId: string;
  timestamp: string;
  data: {
    cpu: number;
    memory: number;
    disk: number;
    network: number;
  };
}
```

**Use Case:**
```typescript
// Live метрики компонент
function LiveMetrics({ serverId }: { serverId: string }) {
  const [metrics, setMetrics] = useState<any>(null);
  const wsRef = useRef<MetricsWebSocket | null>(null);

  useEffect(() => {
    wsRef.current = new MetricsWebSocket();
    wsRef.current.connect(serverId, (data) => {
      setMetrics(data);
    });

    return () => {
      wsRef.current?.disconnect();
    };
  }, [serverId]);

  return (
    <div className="live-metrics">
      <div className="live-indicator">🔴 LIVE</div>
      {metrics && (
        <>
          <div>CPU: {metrics.data.cpu.toFixed(1)}%</div>
          <div>Memory: {metrics.data.memory.toFixed(1)}%</div>
          <div>Disk: {metrics.data.disk.toFixed(0)}%</div>
          <div>Network: {metrics.data.network.toFixed(2)} MB/s</div>
        </>
      )}
    </div>
  );
}
```

---

## 🛡️ 5. ERROR HANDLING

### Standard Error Response
```typescript
interface ErrorResponse {
  message: string;
  errors?: Array<{
    propertyName: string;
    errorMessage: string;
  }>;
}
```

### HTTP Status Codes
- `200` - Success
- `204` - No Content (успешное удаление)
- `400` - Bad Request (некорректные данные)
- `401` - Unauthorized (нет токена или токен невалидный)
- `403` - Forbidden (нет прав доступа)
- `404` - Not Found (ресурс не найден)
- `409` - Conflict (дубликат)
- `500` - Internal Server Error

### Error Handling Example
```typescript
async function apiRequest<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  const token = localStorage.getItem('jwt_token');
  
  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': token ? `Bearer ${token}` : '',
      ...options.headers,
    },
  });

  if (!response.ok) {
    const error: ErrorResponse = await response.json();
    
    switch (response.status) {
      case 401:
        // Токен истек - перенаправляем на login
        localStorage.removeItem('jwt_token');
        window.location.href = '/login';
        break;
      case 403:
        throw new Error('Access denied');
      case 404:
        throw new Error('Resource not found');
      case 409:
        throw new Error(error.message || 'Conflict');
      default:
        throw new Error(error.message || 'Request failed');
    }
  }

  return response.json();
}
```

---

## 📦 6. TYPESCRIPT INTERFACES

### Complete Type Definitions
```typescript
// Auth
interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
}

interface UserResponse {
  id: string;
  userName: string;
  email: string;
  createdAt: string;
}

// Servers
interface ServerResponse {
  id: string;
  serverId: string;
  hostname: string;
  operatingSystem: string;
  accessLevel: number;
  addedAt: string;
  lastSeen: string;
  isActive: boolean;
}

interface AddServerRequest {
  serverKey: string;
}

interface ShareServerRequest {
  targetUserEmail: string;
  accessLevel: number;
}

// Metrics
interface MetricsResponse {
  serverId: string;
  serverName: string;
  startTime: string;
  endTime: string;
  granularity: string;
  dataPoints: DataPoint[];
  totalPoints: number;
}

interface DataPoint {
  timestamp: string;
  cpu: MetricValue;
  memory: MetricValue;
  disk: MetricValue;
  network: MetricValue;
}

interface MetricValue {
  avg: number;
  max: number;
  min: number;
}

// WebSocket
interface WebSocketTokenResponse {
  token: string;
  wsUrl: string;
  expiresAt: string;
}

interface WebSocketMessage {
  type: 'metrics' | 'status' | 'error';
  serverId: string;
  timestamp: string;
  data: {
    cpu: number;
    memory: number;
    disk: number;
    network: number;
  };
}

// Errors
interface ErrorResponse {
  message: string;
  errors?: Array<{
    propertyName: string;
    errorMessage: string;
  }>;
}
```

---

## 🎯 7. QUICK START EXAMPLE

### Complete React Example
```typescript
import React, { useState, useEffect } from 'react';

const API_BASE_URL = 'http://localhost:5246';

function App() {
  const [token, setToken] = useState<string | null>(
    localStorage.getItem('jwt_token')
  );
  const [servers, setServers] = useState<ServerResponse[]>([]);

  // Login
  const handleLogin = async (email: string, password: string) => {
    const response = await fetch(`${API_BASE_URL}/api/users/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    const data: AuthResponse = await response.json();
    localStorage.setItem('jwt_token', data.token);
    setToken(data.token);
  };

  // Get servers
  useEffect(() => {
    if (!token) return;

    const fetchServers = async () => {
      const response = await fetch(`${API_BASE_URL}/api/monitoredservers`, {
        headers: { 'Authorization': `Bearer ${token}` },
      });

      const data: ServerResponse[] = await response.json();
      setServers(data);
    };

    fetchServers();
  }, [token]);

  // Add server
  const handleAddServer = async (serverKey: string) => {
    const response = await fetch(`${API_BASE_URL}/api/monitoredservers/add`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ serverKey }),
    });

    const newServer: ServerResponse = await response.json();
    setServers([...servers, newServer]);
  };

  if (!token) {
    return <LoginForm onLogin={handleLogin} />;
  }

  return (
    <div>
      <h1>My Servers</h1>
      {servers.map(server => (
        <ServerCard key={server.id} server={server} token={token} />
      ))}
      <AddServerForm onAdd={handleAddServer} />
    </div>
  );
}

function ServerCard({ server, token }: { server: ServerResponse; token: string }) {
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      const response = await fetch(
        `${API_BASE_URL}/api/servers/${server.serverId}/metrics/dashboard`,
        {
          headers: { 'Authorization': `Bearer ${token}` },
        }
      );

      const data: MetricsResponse = await response.json();
      setMetrics(data);
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 60000);
    return () => clearInterval(interval);
  }, [server.serverId, token]);

  if (!metrics) return <div>Loading...</div>;

  const latest = metrics.dataPoints[metrics.dataPoints.length - 1];

  return (
    <div className="server-card">
      <h3>{server.hostname}</h3>
      <p>OS: {server.operatingSystem}</p>
      <p>Status: {server.isActive ? '🟢 Online' : '🔴 Offline'}</p>
      {latest && (
        <div className="metrics">
          <div>CPU: {latest.cpu.avg.toFixed(1)}%</div>
          <div>Memory: {latest.memory.avg.toFixed(1)}%</div>
          <div>Disk: {latest.disk.avg.toFixed(0)}%</div>
        </div>
      )}
    </div>
  );
}

export default App;
```

---

## 🔍 8. TESTING

### Test Credentials
```typescript
// Development test user
const TEST_USER = {
  email: 'finalvalidation@example.com',
  password: 'Test123!@#'
};

// Test server
const TEST_SERVER_KEY = 'key_54b8ab51';
const TEST_SERVER_ID = 'srv_a3d881f1';
```

### Test Endpoints with cURL
```bash
# 1. Login
curl -X POST http://localhost:5246/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"finalvalidation@example.com","password":"Test123!@#"}'

# 2. Get servers
curl -X GET http://localhost:5246/api/monitoredservers \
  -H "Authorization: Bearer {token}"

# 3. Dashboard metrics
curl -X GET http://localhost:5246/api/servers/srv_a3d881f1/metrics/dashboard \
  -H "Authorization: Bearer {token}"

# 4. WebSocket token
curl -X POST http://localhost:5246/api/servers/srv_a3d881f1/metrics/live-token \
  -H "Authorization: Bearer {token}"
```

---

## ✅ CHECKLIST

### Before Production
- [ ] Заменить `http://localhost:5246` на production URL
- [ ] Заменить `ws://localhost:8080` на production WebSocket URL
- [ ] Настроить CORS на бэкэнде
- [ ] Добавить refresh token logic
- [ ] Добавить error boundary
- [ ] Настроить rate limiting на фронтенде
- [ ] Добавить loading states
- [ ] Добавить retry logic для failed requests
- [ ] Настроить WebSocket reconnection
- [ ] Добавить analytics/monitoring

---

## 📞 SUPPORT

### Backend Issues
- C# Backend: `http://localhost:5246`
- Swagger UI: `http://localhost:5246/swagger`

### Common Issues
1. **401 Unauthorized** - Проверьте токен и его срок действия
2. **403 Forbidden** - Проверьте права доступа к серверу
3. **404 Not Found** - Проверьте serverId (должен быть `srv_*` формат)
4. **WebSocket disconnects** - Токен истек, получите новый

---

## 🎉 READY TO INTEGRATE!

Все endpoints протестированы и готовы к использованию!

```
✅ Authentication - 100%
✅ Server Management - 100%
✅ Dashboard Metrics - 100%
✅ Realtime Metrics - 100%
✅ Historical Metrics - 100%
✅ WebSocket Streaming - 100%
```

**Happy Coding! 🚀**
