import { apiClient } from '@/lib/api';
import { DiscoveredServersResponse, ImportServersRequest, ImportServersResponse } from '@/types';

export class ServerDiscoveryAPI {
  async findTelegramServers(): Promise<DiscoveredServersResponse> {
    const response = await apiClient.get<DiscoveredServersResponse>('/servers/discovery/telegram');
    return response;
  }

  async importServers(serverIds: string[]): Promise<ImportServersResponse> {
    const request: ImportServersRequest = {
      server_ids: serverIds,
    };

    const response = await apiClient.post<ImportServersResponse>(
      '/servers/discovery/import',
      request,
    );

    return response;
  }
}

export const serverDiscoveryApi = new ServerDiscoveryAPI();
