import { trace, context } from '@opentelemetry/api';

export enum LogLevel {
  DEBUG = 'debug',
  INFO = 'info',
  WARN = 'warn',
  ERROR = 'error',
}

interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: string;
  traceId?: string;
  spanId?: string;
  context?: Record<string, unknown>;
  error?: Error;
}

class StructuredLogger {
  private serviceName: string;

  constructor(serviceName: string = 'servereye-frontend') {
    this.serviceName = serviceName;
  }

  private createLogEntry(
    level: LogLevel,
    message: string,
    context?: Record<string, unknown>,
    error?: Error,
  ): LogEntry {
    const span = trace.getActiveSpan();
    const spanContext = span?.spanContext();

    return {
      level,
      message,
      timestamp: new Date().toISOString(),
      traceId: spanContext?.traceId,
      spanId: spanContext?.spanId,
      context: {
        service: this.serviceName,
        environment: process.env.NODE_ENV,
        ...context,
      },
      error: error
        ? ({
            name: error.name,
            message: error.message,
            stack: error.stack,
          } as unknown as Error)
        : undefined,
    };
  }

  private log(entry: LogEntry): void {
    if (typeof window === 'undefined') {
      return;
    }

    // Always log to console in development for debugging
    if (process.env.NODE_ENV === 'development') {
      const style = this.getConsoleStyle(entry.level);
      if (entry.error) {
      }
    }

    // Send to Loki via Alloy (OTLP HTTP endpoint)
    if (process.env.NEXT_PUBLIC_OTEL_EXPORTER_OTLP_ENDPOINT) {
      const otlpLog = {
        resourceLogs: [
          {
            resource: {
              attributes: [
                { key: 'service.name', value: { stringValue: this.serviceName } },
                { key: 'service.version', value: { stringValue: '1.0.0' } },
                {
                  key: 'deployment.environment',
                  value: { stringValue: process.env.NODE_ENV || 'development' },
                },
              ],
            },
            scopeLogs: [
              {
                scope: {
                  name: this.serviceName,
                  version: '1.0.0',
                },
                logRecords: [
                  {
                    timeUnixNano: String(Date.now() * 1000000),
                    severityNumber: this.getSeverityNumber(entry.level),
                    severityText: entry.level.toUpperCase(),
                    body: { stringValue: entry.message },
                    attributes: [
                      ...(entry.traceId
                        ? [{ key: 'trace_id', value: { stringValue: entry.traceId } }]
                        : []),
                      ...(entry.spanId
                        ? [{ key: 'span_id', value: { stringValue: entry.spanId } }]
                        : []),
                      ...(entry.context
                        ? Object.entries(entry.context).map(([key, value]) => ({
                            key,
                            value: {
                              stringValue:
                                typeof value === 'object' ? JSON.stringify(value) : String(value),
                            },
                          }))
                        : []),
                      ...(entry.error
                        ? [
                            { key: 'error.type', value: { stringValue: entry.error.name } },
                            { key: 'error.message', value: { stringValue: entry.error.message } },
                            { key: 'error.stack', value: { stringValue: entry.error.stack || '' } },
                          ]
                        : []),
                    ],
                  },
                ],
              },
            ],
          },
        ],
      };

      fetch(`${process.env.NEXT_PUBLIC_OTEL_EXPORTER_OTLP_ENDPOINT}/v1/logs`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(otlpLog),
        keepalive: true,
      }).catch(() => {
        // Silently ignore logging errors to prevent infinite loops
      });
    }
  }

  private getSeverityNumber(level: LogLevel): number {
    switch (level) {
      case LogLevel.DEBUG:
        return 5; // DEBUG
      case LogLevel.INFO:
        return 9; // INFO
      case LogLevel.WARN:
        return 13; // WARN
      case LogLevel.ERROR:
        return 17; // ERROR
      default:
        return 9;
    }
  }

  private getConsoleStyle(level: LogLevel): string {
    switch (level) {
      case LogLevel.DEBUG:
        return 'color: #888; font-weight: bold;';
      case LogLevel.INFO:
        return 'color: #0066cc; font-weight: bold;';
      case LogLevel.WARN:
        return 'color: #ff9900; font-weight: bold;';
      case LogLevel.ERROR:
        return 'color: #cc0000; font-weight: bold;';
      default:
        return 'color: #000; font-weight: bold;';
    }
  }

  debug(message: string, context?: Record<string, unknown>): void {
    this.log(this.createLogEntry(LogLevel.DEBUG, message, context));
  }

  info(message: string, context?: Record<string, unknown>): void {
    this.log(this.createLogEntry(LogLevel.INFO, message, context));
  }

  warn(message: string, context?: Record<string, unknown>): void {
    this.log(this.createLogEntry(LogLevel.WARN, message, context));
  }

  error(message: string, error?: Error, context?: Record<string, unknown>): void {
    this.log(this.createLogEntry(LogLevel.ERROR, message, context, error));
  }
}

export const logger = new StructuredLogger();
