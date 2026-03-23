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
    error?: Error
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
      error: error ? {
        name: error.name,
        message: error.message,
        stack: error.stack,
      } as unknown as Error : undefined,
    };
  }

  private log(entry: LogEntry): void {
    if (typeof window === 'undefined') {
      return;
    }

    const logData = JSON.stringify(entry);

    if (process.env.NODE_ENV === 'development') {
      const style = this.getConsoleStyle(entry.level);
      console.log(`%c[${entry.level.toUpperCase()}]`, style, entry.message, entry.context);
      if (entry.error) {
        console.error(entry.error);
      }
    }

    if (process.env.NEXT_PUBLIC_OTEL_EXPORTER_OTLP_ENDPOINT) {
      fetch(`${process.env.NEXT_PUBLIC_OTEL_EXPORTER_OTLP_ENDPOINT}/v1/logs`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: logData,
      }).catch(() => {
        // Ignore errors
      });
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
