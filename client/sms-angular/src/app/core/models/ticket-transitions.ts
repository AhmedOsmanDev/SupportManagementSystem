import { TicketStatus } from './ticket-status';
import { UserRole } from './user-role';

export function allowedStatusTransitions(current: TicketStatus, role: UserRole): TicketStatus[] {
  if (role === 'Customer') return current === 'Resolved' ? ['Closed'] : [];
  const transitions: Record<TicketStatus, TicketStatus[]> = {
    Open: ['InProgress'],
    InProgress: ['Resolved'],
    Resolved: ['InProgress', 'Closed'],
    Closed: [],
  };
  return transitions[current] ?? [];
}

