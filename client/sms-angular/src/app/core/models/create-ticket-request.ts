import { TicketPriority } from './ticket-priority';

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
}

