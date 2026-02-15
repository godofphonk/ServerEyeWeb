// User types
export interface User {
  id: string;
  email: string;
  username: string;
  role: 'user' | 'admin';
  createdAt: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

// Backend User DTO types
export interface BackendUser {
  id: string;
  userName: string;
  email: string;
  serverId?: string;
}

export interface BackendAuthResponse {
  user: BackendUser;
  token: string;
  refreshToken: string;
  expiresIn: number;
}

// Server types (legacy - keep for backward compatibility)
export interface Server {
  id: string;
  userId: string;
  name: string;
  hostname: string;
  ipAddress: string;
  os: string;
  status: 'online' | 'offline';
  apiKey: string;
  lastHeartbeat: string;
  tags?: string[] | null;
  createdAt: string;
  updatedAt: string;
}

// Backend API Server types (new)
export type AccessLevel = 'Owner' | 'Admin' | 'Viewer';

export interface MonitoredServer {
  id: string;
  serverId: string; // srv_xxxxx format
  hostname: string;
  operatingSystem: string;
  accessLevel: AccessLevel;
  addedAt: string;
  lastSeen: string;
  isActive: boolean;
  serverName?: string; // Custom server name
}

export interface AddServerRequest {
  serverKey: string;
  serverName?: string;
}

export interface ShareServerRequest {
  userEmail: string;
  accessLevel: 'Admin' | 'Viewer';
}

// Metrics types (legacy)
export interface Metric {
  serverId: string;
  type: string;
  value: number;
  unit: string;
  timestamp: string;
  labels?: Record<string, string> | string;
}

export interface MetricsHistory {
  metrics: Metric[];
  total: number;
}

// Backend API Metrics types (new)
export interface MetricValue {
  avg: number;
  max: number;
  min: number;
}

export interface MetricsDataPoint {
  timestamp: string;
  cpu: MetricValue;
  memory: MetricValue;
  disk: MetricValue;
  network: MetricValue;
  temperature: MetricValue;
  load: MetricValue;
}

export interface MetricsResponse {
  serverId: string;
  timeRange: {
    start: string;
    end: string;
  };
  granularity: string;
  data: MetricsDataPoint[];
  totalPoints: number;
}

export interface CurrentMetrics {
  cpu: number;
  memory: number;
  disk: number;
  network: number;
  temperature: number;
  load: number;
}

export interface MetricTrends {
  cpu: string;
  memory: string;
  disk: string;
  network: string;
  temperature: string;
  load: string;
}

export interface MetricAlert {
  type: 'warning' | 'error';
  message: string;
  timestamp: string;
}

export interface DashboardMetrics {
  serverId: string;
  current: CurrentMetrics;
  trends: MetricTrends;
  alerts: MetricAlert[];
}

// WebSocket types
export interface WebSocketTokenResponse {
  token: string;
  wsUrl: string;
  expiresAt: string;
}

export interface LiveMetrics {
  serverId: string;
  timestamp: string;
  cpu: number;
  memory: number;
  disk: number;
  network: number;
  temperature: number;
  load: number;
}

// Subscription types
export interface Plan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  billingPeriod: 'monthly' | 'yearly';
  maxServers: number;
  maxAgents: number;
  features: string[];
}

export interface Subscription {
  id: string;
  userId: string;
  planId: string;
  status: 'active' | 'cancelled' | 'expired';
  startDate: string;
  endDate: string;
  autoRenew: boolean;
  plan?: Plan;
}

// API Response types
export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface ApiError {
  message: string;
  code: string;
  details?: any;
}

// Form types
export interface LoginFormData {
  email: string;
  password: string;
}

export interface RegisterFormData {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
}

export interface ServerFormData {
  name: string;
  hostname: string;
  ipAddress: string;
  os: string;
  tags: string[];
}
