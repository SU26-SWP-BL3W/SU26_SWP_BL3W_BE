import { apiClient } from '@/services/api';
import { eventsApi } from '../events';

jest.mock('@/services/api', () => ({
  apiClient: {
    get: jest.fn(),
  },
}));

const client = apiClient as jest.Mocked<typeof apiClient>;

describe('eventsApi', () => {
  afterEach(() => jest.resetAllMocks());

  test('requests events sorted by name in ascending order', async () => {
    client.get.mockResolvedValue({ data: { data: [] } } as never);

    await eventsApi.list(true);

    expect(client.get).toHaveBeenCalledWith('/Events', {
      params: {
        PageNumber: 1,
        PageSize: 100,
        SortBy: 'eventName',
        IsAscending: true,
        Status: true,
      },
    });
  });
});
