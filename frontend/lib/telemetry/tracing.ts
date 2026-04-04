import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { registerInstrumentations } from '@opentelemetry/instrumentation';

let provider: WebTracerProvider | null = null;

export function initializeTelemetry() {
  if (typeof window === 'undefined') {
    return;
  }

  if (provider) {
    return provider;
  }

  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: 'servereye-frontend',
    [ATTR_SERVICE_VERSION]: '1.0.0',
    environment: process.env.NODE_ENV || 'development',
  });

  const otlpExporter = new OTLPTraceExporter({
    url: process.env.NEXT_PUBLIC_OTEL_EXPORTER_OTLP_ENDPOINT || 'http://localhost:4318/v1/traces',
    headers: {},
  });

  provider = new WebTracerProvider({
    resource,
    spanProcessors: [
      new BatchSpanProcessor(otlpExporter, {
        maxQueueSize: 100,
        maxExportBatchSize: 10,
        scheduledDelayMillis: 500,
        exportTimeoutMillis: 30000,
      }),
    ],
  });

  provider.register();

  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: [/localhost/, /127\.0\.0\.1/, /servereye\.com/],
        clearTimingResources: true,
        applyCustomAttributesOnSpan: (span, request, result) => {
          if (request instanceof Request) {
            span.setAttribute('http.url', request.url);
            span.setAttribute('http.method', request.method);
          }
          if (result instanceof Response) {
            span.setAttribute('http.status_code', result.status);
          }
        },
      }),
      new XMLHttpRequestInstrumentation({
        propagateTraceHeaderCorsUrls: [/localhost/, /127\.0\.0\.1/, /servereye\.com/],
      }),
    ],
  });

  return provider;
}

export function getTracer(name: string = 'servereye-frontend') {
  if (!provider) {
    initializeTelemetry();
  }
  return provider?.getTracer(name);
}
