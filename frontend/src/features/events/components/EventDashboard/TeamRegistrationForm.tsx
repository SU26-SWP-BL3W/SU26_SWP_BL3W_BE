'use client';

import React, { useState } from 'react';
import { X } from 'lucide-react';
import { Button } from './Button';
import { useEventDashboard } from '../../contexts/EventDashboardContext';
import { useCreateTeam } from '@/features/events/hooks/useEvents';

interface TeamMember {
  id: string;
  name: string;
  email: string;
}

const mockUsers: TeamMember[] = [
  { id: 'user-1', name: 'Alice Johnson', email: 'alice@example.com' },
  { id: 'user-2', name: 'Bob Smith', email: 'bob@example.com' },
  { id: 'user-3', name: 'Charlie Wilson', email: 'charlie@example.com' },
  { id: 'user-4', name: 'Diana Lee', email: 'diana@example.com' },
  { id: 'user-5', name: 'Eve Martinez', email: 'eve@example.com' },
];

interface TeamRegistrationFormProps {
  eventId: string;
  userId: string;
}

export function TeamRegistrationForm({ eventId, userId }: TeamRegistrationFormProps) {
  const { setIsModalOpen } = useEventDashboard();
  const createTeamMutation = useCreateTeam(eventId);

  const [teamName, setTeamName] = useState('');
  const [selectedMembers, setSelectedMembers] = useState<TeamMember[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showMemberDropdown, setShowMemberDropdown] = useState(false);
  const [submitError, setSubmitError] = useState<string>('');

  const filteredUsers = mockUsers.filter(
    (user) =>
      user.id !== userId &&
      !selectedMembers.find((m) => m.id === user.id) &&
      (user.name.toLowerCase().includes(searchInput.toLowerCase()) ||
        user.email.toLowerCase().includes(searchInput.toLowerCase()))
  );

  const handleAddMember = (member: TeamMember) => {
    setSelectedMembers([...selectedMembers, member]);
    setSearchInput('');
    setShowMemberDropdown(false);
  };

  const handleRemoveMember = (memberId: string) => {
    setSelectedMembers(selectedMembers.filter((m) => m.id !== memberId));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!teamName.trim()) {
      newErrors.teamName = 'Team name is required';
    }

    if (selectedMembers.length === 0) {
      newErrors.members = 'Add at least one team member';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError('');

    if (!validateForm()) return;

    const memberIds = [userId, ...selectedMembers.map((m) => m.id)];

    try {
      await createTeamMutation.mutateAsync({
        name: teamName,
        memberIds,
      });

      setTeamName('');
      setSelectedMembers([]);
      setErrors({});
      setIsModalOpen(false);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Failed to create team');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col h-full">
      <div className="space-y-6 overflow-y-auto flex-1 p-6 md:p-8">
        {submitError && (
          <div className="bg-error/10 border border-error rounded-sm p-3">
            <p className="t-body-sm text-error font-bold">{submitError}</p>
          </div>
        )}

        {/* Team Name */}
        <div>
          <label htmlFor="teamName" className="block t-body-strong text-ink mb-2 text-sm md:text-base">
            Team Name
          </label>
          <input
            id="teamName"
            type="text"
            placeholder="Enter team name"
            value={teamName}
            onChange={(e) => {
              setTeamName(e.target.value);
              if (errors.teamName) setErrors({ ...errors, teamName: '' });
            }}
            aria-invalid={!!errors.teamName}
            aria-describedby={errors.teamName ? 'teamName-error' : undefined}
            className={`w-full px-4 py-3 md:py-2 border rounded-sm text-base md:text-body-md focus:outline-none transition-colors duration-150 min-h-12 md:min-h-11 ${
              errors.teamName ? 'border-error focus:border-error' : 'border-hairline focus:border-primary'
            }`}
          />
          {errors.teamName && (
            <p id="teamName-error" className="t-body-sm text-error mt-1" role="alert">
              {errors.teamName}
            </p>
          )}
        </div>

        {/* Team Members */}
        <div>
          <label className="block t-body-strong text-ink mb-2">Add Members</label>

          {/* Current User (Leader) */}
          <div className="mb-3 p-3 bg-surface-soft rounded-sm">
            <p className="t-body-sm text-mute uppercase font-bold text-xs">Trưởng nhóm (Bạn)</p>
            <p className="t-body-strong text-ink">Current User</p>
          </div>

          {/* Member Search */}
          <div className="relative mb-3">
            <input
              type="text"
              placeholder="Search by name or email"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onFocus={() => setShowMemberDropdown(true)}
              className="w-full px-4 py-3 md:py-2 border border-hairline rounded-sm text-base md:text-body-md focus:border-primary focus:outline-none min-h-12 md:min-h-11"
            />

            {/* Dropdown */}
            {showMemberDropdown && filteredUsers.length > 0 && (
              <div className="absolute top-full left-0 right-0 mt-1 bg-canvas border border-hairline rounded-sm shadow-lg z-10">
                {filteredUsers.map((user) => (
                  <button
                    key={user.id}
                    type="button"
                    onClick={() => handleAddMember(user)}
                    className="w-full text-left px-4 py-2 hover:bg-surface-soft transition-colors duration-150 border-b border-hairline last:border-b-0"
                  >
                    <p className="t-body-sm font-bold text-ink">{user.name}</p>
                    <p className="t-caption-sm text-mute">{user.email}</p>
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Selected Members */}
          {selectedMembers.length > 0 && (
            <div className="flex flex-wrap gap-2 mb-3">
              {selectedMembers.map((member) => (
                <div
                  key={member.id}
                  className="inline-flex items-center gap-2 bg-surface-soft text-ink px-3 py-1 rounded-full text-caption-xs"
                >
                  <span>{member.name}</span>
                  <button
                    type="button"
                    onClick={() => handleRemoveMember(member.id)}
                    className="ml-1 hover:text-error transition-colors duration-150 focus-visible:outline-1 focus-visible:outline-primary"
                    aria-label={`Remove ${member.name} from team`}
                  >
                    <X size={12} aria-hidden="true" />
                  </button>
                </div>
              ))}
            </div>
          )}

          {errors.members && (
            <p className="t-body-sm text-error" role="alert">
              {errors.members}
            </p>
          )}
        </div>
      </div>

      {/* Modal Footer */}
      <div className="flex gap-3 justify-end p-6 md:p-8 border-t border-hairline flex-shrink-0 bg-canvas">
        <Button
          variant="secondary"
          onClick={() => setIsModalOpen(false)}
          disabled={createTeamMutation.isPending}
          type="button"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          variant="create"
          isLoading={createTeamMutation.isPending}
          disabled={!teamName.trim() || selectedMembers.length === 0}
        >
          Create Team
        </Button>
      </div>
    </form>
  );
}
