import { apiClient } from '@/services/api';
import { prizesApi } from '../prizes';

jest.mock('@/services/api', () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
    patch: jest.fn(),
  },
}));

const client = apiClient as jest.Mocked<typeof apiClient>;

describe('prizesApi', () => {
  afterEach(() => jest.resetAllMocks());

  test('lists prizes and encodes the event id', async () => {
    const prizes = [{ id: 'p1', eventId: 'e/1', prizeName: 'Giải Nhất', value: '10 triệu', quantity: 1 }];
    client.get.mockResolvedValue({ data: prizes } as never);
    await expect(prizesApi.listByEvent('e/1')).resolves.toEqual(prizes);
    expect(client.get).toHaveBeenCalledWith('/Events/e%2F1/Prizes');
  });

  test('creates and updates a prize', async () => {
    const payload = { prizeName: 'Giải Nhì', value: '5 triệu', quantity: 2 };
    client.post.mockResolvedValue({ data: { id: 'p2', eventId: 'e1', ...payload } } as never);
    client.put.mockResolvedValue({ data: null } as never);

    await prizesApi.create('e1', payload);
    await prizesApi.update('p/2', payload);

    expect(client.post).toHaveBeenCalledWith('/Events/e1/Prizes', payload);
    expect(client.put).toHaveBeenCalledWith('/Prizes/p%2F2', payload);
  });

  test('removes and assigns a prize, including clearing the assignment', async () => {
    client.delete.mockResolvedValue({ data: null } as never);
    client.patch.mockResolvedValue({ data: null } as never);

    await prizesApi.remove('p1');
    await prizesApi.assignToResult('r/1', null);

    expect(client.delete).toHaveBeenCalledWith('/Prizes/p1');
    expect(client.patch).toHaveBeenCalledWith('/FinalResults/r%2F1/assign-prize', { prizeId: null });
  });
});
