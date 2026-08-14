import { TicketPriority } from './ticket-priority';
import { TicketStatus } from './ticket-status';

export interface TicketQuery {
  page: number;
  pageSize: number;
  search?: string;
  status?: TicketStatus | '';
  priority?: TicketPriority | '';
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

