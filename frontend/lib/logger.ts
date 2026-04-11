interface Logger {
  log: (...args: unknown[]) => void;
  warn: (...args: unknown[]) => void;
  error: (...args: unknown[]) => void;
  debug: (...args: unknown[]) => void;
  info: (...args: unknown[]) => void;
}

const createLogger = (): Logger => {
  const noop = () => {};

  return {
    log: noop,
    warn: noop,
    error: noop,
    debug: noop,
    info: noop,
  };
};

export const logger = createLogger();

export default logger;
