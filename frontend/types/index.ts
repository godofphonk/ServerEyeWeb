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
}

// Server types
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

// Metrics types
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
