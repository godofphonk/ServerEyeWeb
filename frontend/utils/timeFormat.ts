export const formatTimeByRange = (timestamp: string, timeRange: string): string => {
  const date = new Date(timestamp);
  
  switch (timeRange) {
    case '1h':
      // Показываем минуты и секунды
      return date.toLocaleTimeString('ru-RU', { 
        hour: '2-digit', 
        minute: '2-digit' 
      });
    
    case '6h':
      // Показываем часы и минуты
      return date.toLocaleTimeString('ru-RU', { 
        hour: '2-digit', 
        minute: '2-digit' 
      });
    
    case '24h':
      // Показываем часы и минуты
      return date.toLocaleTimeString('ru-RU', { 
        hour: '2-digit', 
        minute: '2-digit' 
      });
    
    case '7d':
      // Показываем день и час
      return date.toLocaleString('ru-RU', { 
        month: 'short', 
        day: 'numeric', 
        hour: '2-digit', 
        minute: '2-digit' 
      });
    
    case '30d':
      // Показываем дату
      return date.toLocaleDateString('ru-RU', { 
        month: 'short', 
        day: 'numeric' 
      });
    
    default:
      return date.toLocaleString('ru-RU');
  }
};

export const getChartDateFormat = (timeRange: string): string => {
  switch (timeRange) {
    case '1h':
    case '6h':
    case '24h':
      return 'HH:mm';
    
    case '7d':
      return 'dd MMM HH:mm';
    
    case '30d':
      return 'dd MMM';
    
    default:
      return 'dd MMM HH:mm';
  }
};

export const getTickCountByRange = (timeRange: string): number => {
  switch (timeRange) {
    case '1h':
      return 6;
    case '6h':
      return 6;
    case '24h':
      return 8;
    case '7d':
      return 7;
    case '30d':
      return 10;
    default:
      return 6;
  }
};
