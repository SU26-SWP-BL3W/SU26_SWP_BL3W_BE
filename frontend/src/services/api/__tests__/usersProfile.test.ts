import { usersApi } from '../users';
import apiClient from '@/services/api/client';

jest.mock('@/services/api/client');
const mocked = apiClient as jest.Mocked<typeof apiClient>;

describe('usersApi — profile/approve/reject', () => {
  afterEach(() => jest.resetAllMocks());

  test('getProfile GETs /Users/profile and returns data', async () => {
    const profile = {
      id: 'u1',
      email: 'a@b.com',
      fullName: 'Nguyen Van A',
      studentCode: 'SE123456',
      schoolId: 'school1',
      isStudent: true,
      isAdmin: false,
      isApproved: false,
      isFpt: true,
      photoStudentCardUrl: null,
    };
    mocked.get.mockResolvedValue({ data: profile } as never);

    const result = await usersApi.getProfile();

    expect(mocked.get).toHaveBeenCalledWith('/Users/profile');
    expect(result).toEqual(profile);
  });

  test('approve POSTs /Users/{id}/approve and returns void', async () => {
    mocked.post.mockResolvedValue({ data: {} } as never);

    const result = await usersApi.approve('u1');

    expect(mocked.post).toHaveBeenCalledWith('/Users/u1/approve');
    expect(result).toBeUndefined();
  });

  test('approve URL-encodes the id', async () => {
    mocked.post.mockResolvedValue({ data: {} } as never);

    await usersApi.approve('u/id with spaces');

    expect(mocked.post).toHaveBeenCalledWith('/Users/u%2Fid%20with%20spaces/approve');
  });

  test('reject POSTs /Users/{id}/reject with {reason}', async () => {
    mocked.post.mockResolvedValue({ data: {} } as never);

    const result = await usersApi.reject('u2', 'sai MSSV');

    expect(mocked.post).toHaveBeenCalledWith('/Users/u2/reject', { reason: 'sai MSSV' });
    expect(result).toBeUndefined();
  });

  test('reject URL-encodes the id', async () => {
    mocked.post.mockResolvedValue({ data: {} } as never);

    await usersApi.reject('u/special', 'bad');

    expect(mocked.post).toHaveBeenCalledWith('/Users/u%2Fspecial/reject', { reason: 'bad' });
  });
});
