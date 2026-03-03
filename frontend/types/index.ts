// User types
export interface User {
  id: string;
  email: string;
  username: string;
  role: 'user' | 'admin';
  createdAt: string;
  isEmailVerified?: boolean;
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
  role?: 'user' | 'admin' | string;
  serverId?: string;
  isEmailVerified?: boolean; // <-- новое поле
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

// Static server info from new endpoint
export interface CpuInfo {
  model: string;
  cores: number;
  threads: number;
  frequency_mhz: number;
}

export interface MemoryInfo {
  total_gb: number;
  type: string;
  speed_mhz: number;
}

export interface DiskInfo {
  device: string;
  size_gb: number;
  type: string;
  model?: string;
}

export interface NetworkInterface {
  name: string;
  rx_bytes: number;
  tx_bytes: number;
  rx_packets: number;
  tx_packets: number;
  rx_speed_mbps: number;
  tx_speed_mbps: number;
  status: 'up' | 'down';
}

export interface ServerStaticInfo {
  server_id: string;
  hostname: string;
  operating_system: string;
  agent_version: string;
  cpu_info: CpuInfo;
  memory_info: MemoryInfo;
  disk_info: DiskInfo[];
  network_interfaces: NetworkInterface[];
  last_updated: string;
}

// Backend API Server types (new)
export type AccessLevel = 'Owner' | 'Admin' | 'Viewer';

export interface MonitoredServer {
  id: string;
  serverId: string; // srv_xxxxx format
  serverKey: string; // key_xxxxx format for static info endpoint
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

export interface TemperatureDetails {
  cpu_temperature: number;
  gpu_temperature: number;
  system_temperature: number;
  storage_temperatures: any[] | null;
  highest_temperature: number;
  temperature_unit: 'celsius' | 'fahrenheit';
}

export interface NetworkInterface {
  name: string;
  rx_bytes: number;
  tx_bytes: number;
  rx_packets: number;
  tx_packets: number;
  rx_speed_mbps: number;
  tx_speed_mbps: number;
  status: 'up' | 'down';
}

export interface NetworkDetails {
  interfaces: NetworkInterface[];
  total_rx: number;
  total_tx: number;
  timestamp: string;
}

export interface MetricsDataPoint {
  timestamp: string;
  cpu: MetricValue;
  memory: MetricValue;
  disk: MetricValue;
  network: MetricValue;
  temperature: MetricValue;
  loadAverage: MetricValue; // API returns loadAverage, not load
  cpu_frequency?: MetricValue; // CPU frequency in MHz
  temperature_details?: TemperatureDetails | null; // Detailed temperature info
  // Raw backend fields
  cpu_avg?: number;
  cpu_max?: number;
  cpu_min?: number;
  memory_avg?: number;
  memory_max?: number;
  memory_min?: number;
  disk_avg?: number;
  disk_max?: number;
  disk_min?: number;
  network_avg?: number;
  network_max?: number;
  network_min?: number;
  temp_avg?: number;
  temp_max?: number;
  temp_min?: number;
  load_avg?: number;
  load_max?: number;
  load_min?: number;
  sample_count?: number;
}

export interface HistoricalMetricsResponse {
  server_id: string;
  start_time: string;
  end_time: string;
  granularity: string;
  data_points: {
    timestamp: string;
    cpu_avg: number;
    cpu_max: number;
    cpu_min: number;
    memory_avg: number;
    memory_max: number;
    memory_min: number;
    disk_avg: number;
    disk_max: number;
    network_avg: number;
    network_max: number;
    temp_avg: number;
    temp_max: number;
    load_avg: number;
    load_max: number;
    sample_count: number;
  }[];
  total_points: number;
}

export interface MetricsResponse {
  serverId: string;
  serverName?: string;
  timeRange: {
    start: string;
    end: string;
  };
  granularity: string;
  data: MetricsDataPoint[];
  totalPoints: number;
  summary?: {
    avgCpu?: number;
    avgMemory?: number;
    avgDisk?: number;
    avgNetwork?: number;
    avgLoad?: number;
    avgTemperature?: number;
    minCpu?: number;
    maxCpu?: number;
    minMemory?: number;
    maxMemory?: number;
    minDisk?: number;
    maxDisk?: number;
    totalDataPoints?: number;
    timeRange?: string;
  } | null;
  message?: string | null;
  isCached?: boolean;
  startTime?: string;
  endTime?: string;
  // Backend fields
  server_id?: string;
  server_name?: string;
  start_time?: string;
  end_time?: string;
  data_points?: any[];
  total_points?: number;
  status?: any;
  cached_at?: string;
}

export interface CurrentMetrics {
  cpu: number;
  memory: number;
  disk: number;
  network: number;
  temperature: number;
  gpu_temperature?: number;
  load: number;
}

export interface MetricTrends {
  cpu: number;
  memory: number;
  disk: number;
  network: number;
  temperature: number;
  load: number;
}

export interface MetricAlert {
  type: 'warning' | 'error';
  message: string;
  timestamp: string;
}

export interface DashboardMetrics {
  serverId?: string;
  current: CurrentMetrics;
  trends: MetricTrends;
  alerts?: MetricAlert[];
  timestamp?: string;
  summary?: any;
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

// Support Ticket types
export enum TicketStatus {
  New = 0,
  Open = 1,
  InProgress = 2,
  Resolved = 3,
  Closed = 4,
  Reopened = 5,
}

export enum TicketPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export interface TicketMessage {
  id: string;
  ticketId: string;
  message: string;
  senderName: string;
  senderEmail: string;
  isStaffReply: boolean;
  createdAt: string;
}

export interface Ticket {
  id: string;
  ticketNumber: string;
  name: string;
  email: string;
  subject: string;
  message: string;
  status: TicketStatus;
  statusDisplay: string;
  priority: TicketPriority;
  priorityDisplay: string;
  createdAt: string;
  updatedAt: string | null;
  resolvedAt: string | null;
  closedAt: string | null;
  assignedToUserName: string | null;
  messages: TicketMessage[];
  userId?: string;
  messagesCount?: number;
}

export interface PaginatedTicketsResponse {
  tickets: Ticket[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateTicketRequest {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export interface CreateTicketResponse extends Ticket {}

export interface TicketStatsResponse {
  totalCount: number;
  statusCounts: {
    New: number;
    Open: number;
    InProgress: number;
    Resolved: number;
    Closed: number;
    Reopened: number;
  };
}

export interface TicketStatsDisplay {
  total: number;
  new: number;
  open: number;
  inProgress: number;
  resolved: number;
  closed: number;
  reopened: number;
}

export interface AddTicketMessageRequest {
  message: string;
  senderName: string;
  senderEmail: string;
  isStaffReply: boolean;
}

export interface UpdateTicketStatusRequest {
  status: TicketStatus;
}

// Notification Types
export enum NotificationType {
  TicketCreated = 0,
  StatusChanged = 1,
  NewMessage = 2,
}

export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  ticketId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationResponse {
  notifications: Notification[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
}

export interface UnreadCountResponse {
  count: number;
}

// Email Verification Types
export interface VerifyEmailRequest {
  email: string;
  code: string;
}

export interface ResendVerificationRequest {
  email: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface ChangeEmailRequest {
  newEmail: string;
}

export interface ConfirmEmailChangeRequest {
  code: string;
}

// Account Deletion types
export interface RequestAccountDeletionRequest {
  password: string;
}

export interface ConfirmAccountDeletionRequest {
  confirmationCode: string;
}

// Toast Notification Types
export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  title: string;
  message?: string;
  duration?: number;
}

// OAuth Types
export interface OAuthChallengeRequest {
  provider: 'google' | 'github' | 'telegram';
  returnUrl?: string;
}

export interface OAuthChallengeResponse {
  challengeUrl: string;
  state: string;
  codeVerifier: string;
}

export interface ExternalLogin {
  provider: string;
  providerKey: string;
  providerDisplayName: string;
  isLinked: boolean;
  email?: string; // Email может быть пустым для Telegram пользователей
}

export interface ExternalLoginsResponse {
  externalLogins: ExternalLogin[];
}

export interface LinkOAuthRequest {
  provider: string;
  code: string;
  state: string;
}

export interface OAuthCallbackRequest {
  provider: string;
  code: string;
  state: string;
  codeVerifier?: string;
}
