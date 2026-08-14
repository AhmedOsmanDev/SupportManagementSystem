import { TicketPriority } from './ticket-priority';
import { TicketStatus } from './ticket-status';

export interface TicketSummary {
  number: number;
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
