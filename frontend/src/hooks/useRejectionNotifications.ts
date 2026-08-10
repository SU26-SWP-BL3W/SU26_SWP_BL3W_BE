import { useQuery } from '@tanstack/react-query';
import { userRejectionsApi } from '@/features/registration/api/userRejections';
import { useCurrentUser } from '@/hooks/useAuth';

export function useRejectionNotifications() {
  const { data: user } = useCurrentUser();

  const query = useQuery({
    queryKey: ['my-rejections', user?.id],
    queryFn: () => userRejectionsApi.listForUser(user?.id ?? ''),
    enabled: !!user?.id,
    refetchInterval: 30000,
    refetchOnWindowFocus: true,
  });

  const activeRejections = query.data?.filter(r => r.isActive) || [];

  return {
    ...query,
    activeRejections,
    rejectionCount: activeRejections.length,
  };
}
