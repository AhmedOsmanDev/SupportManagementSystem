export const ticketStatuses = ['Open', 'InProgress', 'Resolved', 'Closed'] as const;
export const ticketPriorities = ['Low', 'Medium', 'High', 'Critical'] as const;

export type TicketStatus = (typeof ticketStatuses)[number];
export type TicketPriority = (typeof ticketPriorities)[number];

export interface TicketSummary {
  number: string;
  title: string;
  description?: string;
  status: TicketStatus;
  priority: TicketPriority;
  customerId?: string;
  customerName?: string;
  assignedAgentId?: string | null;
  assignedAgentName?: string | null;
  createdAt: string;
  updatedAt?: string;
  totalTimeMinutes?: number;
}

export interface TicketComment {
  id: string;
  content: string;
  authorId?: string;
  authorName: string;
  authorRole?: string;
  createdAt: string;
}

export interface TicketActivity {
  id: string;
  type: string;
  description: string;
  performedBy?: string;
  oldValue?: string | null;
  newValue?: string | null;
  createdAt: string;
}

export interface TimeEntry {
  id: string;
  agentId?: string;
  agentName: string;
  workDate: string;
  durationMinutes: number;
  description: string;
  createdAt?: string;
}

export interface TicketDetail extends TicketSummary {
  comments: TicketComment[];
  activities: TicketActivity[];
  timeEntries: TimeEntry[];
  resolvedAt?: string | null;
  closedAt?: string | null;
}

export interface TicketQuery {
  page: number;
  pageSize: number;
  search?: string;
  status?: TicketStatus | '';
  priority?: TicketPriority | '';
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
}

export interface AddCommentRequest {
  content: string;
}

export interface LogTimeRequest {
  workDate: string;
  durationMinutes: number;
  description: string;
}

export function displayStatus(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function formatMinutes(minutes = 0): string {
  if (!minutes) return '0m';
  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  return [hours ? `${hours}h` : '', remainder ? `${remainder}m` : ''].filter(Boolean).join(' ');
}

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
import { UserRole } from './auth.models';
