import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '@/services/api/users';
import { useCurrentUser } from '@/hooks/useAuth';

export function useApprovalNotification() {
  const { data: user } = useCurrentUser();
  const [showApproval, setShowApproval] = useState(false);

  const { data: profile, isLoading, refetch } = useQuery({
    queryKey: ['users', 'profile'],
    queryFn: () => usersApi.getProfile(),
    enabled: !!user && user.role === 'STUDENT',
    staleTime: 30000,
  });

  useEffect(() => {
    const timer = window.setTimeout(() => {
      if (profile?.isApproved) {
        if (!localStorage.getItem('approved_notif_read')) {
          setShowApproval(true);
        }
      } else if (profile && profile.isApproved === false) {
        // Xoá cờ nếu tài khoản bị từ chối, để lần sau được duyệt lại sẽ hiện thông báo
        localStorage.removeItem('approved_notif_read');
        setShowApproval(false);
      }
    }, 0);
    return () => window.clearTimeout(timer);
  }, [profile]);

  const dismissApproval = () => {
    localStorage.setItem('approved_notif_read', 'true');
    setShowApproval(false);
  };

  return {
    showApproval,
    dismissApproval,
    isLoading,
    refetch,
  };
}
