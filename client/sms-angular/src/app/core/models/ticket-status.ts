export const ticketStatuses = ['Open', 'InProgress', 'Resolved', 'Closed'] as const;

export type TicketStatus = (typeof ticketStatuses)[number];

