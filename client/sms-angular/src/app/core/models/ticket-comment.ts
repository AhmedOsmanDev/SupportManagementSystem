export interface TicketComment {
  id: string;
  content: string;
  authorId?: string;
  authorName: string;
  authorRole?: string;
  createdAt: string;
}

