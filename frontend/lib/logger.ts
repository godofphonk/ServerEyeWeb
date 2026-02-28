type LogLevel = 'log' | 'warn' | 'error' | 'debug' | 'info';

interface Logger {
  log: (...args: unknown[]) => void;
  warn: (...args: unknown[]) => void;
  error: (...args: unknown[]) => void;
  debug: (...args: unknown[]) => void;
  info: (...args: unknown[]) => void;
}

const isDevelopment = process.env.NODE_ENV === 'development';
const isDebugMode = process.env.NEXT_PUBLIC_DEBUG_MODE === 'true';

const createLogger = (): Logger => {
  const noop = () => {};

  return {
    log: isDevelopment || isDebugMode ? console.log.bind(console) : noop,
    warn: console.warn.bind(console),
    error: console.error.bind(console),
    debug: isDevelopment || isDebugMode ? console.debug.bind(console) : noop,
    info: isDevelopment || isDebugMode ? console.info.bind(console) : noop,
  };
};

export const logger = createLogger();

export default logger;
