import { appealsApi } from '../appeals';
import { apiClient } from '@/services/api';

jest.mock('@/services/api', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn() },
}));

const client = apiClient as jest.Mocked<typeof apiClient>;

describe('appealsApi', () => {
  beforeEach(() => jest.clearAllMocks());

  test('lists round appeals with Swagger pagination names', async () => {
    client.get.mockResolvedValueOnce({ data: { data: [], currentPage: 2, totalPages: 3, totalItems: 21 } } as never);
    const result = await appealsApi.listByRound('round/1', 2, 10);
    expect(client.get).toHaveBeenCalledWith('/Appeals/round/round%2F1', {
      params: { PageNumber: 2, PageSize: 10, SortBy: 'CreatedTime', IsAscending: false },
    });
    expect(result).toMatchObject({ pageIndex: 2, totalPages: 3, totalItems: 21 });
  });

  test('respond payload includes appealId required by Swagger schema', async () => {
    client.put.mockResolvedValueOnce({ data: { id: 'a-1' } } as never);
    await appealsApi.respond('a-1', { status: 1, response: 'Đồng ý', assignedJudgeId: 'judge-1' });
    expect(client.put).toHaveBeenCalledWith('/Appeals/a-1/respond', {
      appealId: 'a-1', status: 1, response: 'Đồng ý', assignedJudgeId: 'judge-1',
    });
  });

  test('lists appeals assigned to the current judge', async () => {
    client.get.mockResolvedValueOnce({ data: { data: [{ id: 'a-1' }] } } as never);
    await expect(appealsApi.getAssignedAppeals('role/1')).resolves.toEqual([{ id: 'a-1' }]);
    expect(client.get).toHaveBeenCalledWith('/Appeals/assigned/role%2F1');
  });
});
