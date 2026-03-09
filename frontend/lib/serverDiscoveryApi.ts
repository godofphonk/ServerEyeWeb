import { apiClient } from './api';
import {
  DiscoveredServersResponse,
  ImportServersRequest,
  ImportServersResponse,
} from '@/types';

export class ServerDiscoveryAPI {
  async findTelegramServers(): Promise<DiscoveredServersResponse> {
    try {
      const response = await apiClient.get<DiscoveredServersResponse>(
        '/servers/discovery/telegram'
      );
      return response;
    } catch (error: any) {
      console.error('[ServerDiscovery] Failed to find Telegram servers:', error);
      throw error;
    }
  }

  async importServers(serverIds: string[]): Promise<ImportServersResponse> {
    try {
      const request: ImportServersRequest = {
        server_ids: serverIds,
      };
      
      const response = await apiClient.post<ImportServersResponse>(
        '/servers/discovery/import',
        request
      );
      
      return response;
    } catch (error: any) {
      console.error('[ServerDiscovery] Failed to import servers:', error);
      throw error;
    }
  }
}

export const serverDiscoveryApi = new ServerDiscoveryAPI();
