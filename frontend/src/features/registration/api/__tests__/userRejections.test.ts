import { userRejectionsApi } from '../userRejections';
import apiClient from '@/services/api/client';

jest.mock('@/services/api/client');
const mocked = apiClient as jest.Mocked<typeof apiClient>;

describe('userRejectionsApi', () => {
  afterEach(() => jest.resetAllMocks());

  test('listForUser returns the interceptor-unwrapped response', async () => {
    mocked.get.mockResolvedValue({
      data: [{ id: 'r1', userId: 'u1', rejectedBy: 'a1', reason: 'thiếu ảnh', createdTime: '2026-06-23T00:00:00Z' }],
    } as never);
    const rows = await userRejectionsApi.listForUser('u1');
    expect(mocked.get).toHaveBeenCalledWith('/UserRejections/user/u1');
    expect(rows).toHaveLength(1);
    expect(rows[0].reason).toBe('thiếu ảnh');
  });

  test('listForUser returns [] when data missing', async () => {
    mocked.get.mockResolvedValue({ data: undefined } as never);
    expect(await userRejectionsApi.listForUser('u1')).toEqual([]);
  });

  test('remove DELETEs /UserRejections/{id} and returns void', async () => {
    mocked.delete.mockResolvedValue({ data: {} } as never);
    const result = await userRejectionsApi.remove('r1');
    expect(mocked.delete).toHaveBeenCalledWith('/UserRejections/r1');
    expect(result).toBeUndefined();
  });

  test('remove URL-encodes the id', async () => {
    mocked.delete.mockResolvedValue({ data: {} } as never);
    await userRejectionsApi.remove('r/special id');
    expect(mocked.delete).toHaveBeenCalledWith('/UserRejections/r%2Fspecial%20id');
  });
});
