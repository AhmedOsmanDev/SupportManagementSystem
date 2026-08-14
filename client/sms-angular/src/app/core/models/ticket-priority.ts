export const ticketPriorities = ['Low', 'Medium', 'High', 'Critical'] as const;

export type TicketPriority = (typeof ticketPriorities)[number];

