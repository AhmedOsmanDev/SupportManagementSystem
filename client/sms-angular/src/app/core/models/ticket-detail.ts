import { TicketActivity } from './ticket-activity';
import { TicketComment } from './ticket-comment';
import { TicketSummary } from './ticket-summary';
import { TimeEntry } from './time-entry';

export interface TicketDetail extends TicketSummary {
  comments: TicketComment[];
  activities: TicketActivity[];
  timeEntries: TimeEntry[];
  resolvedAt?: string | null;
  closedAt?: string | null;
}

