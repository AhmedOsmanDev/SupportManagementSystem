export interface TicketActivity {
  id: string;
  type: string;
  description: string;
  performedBy?: string;
  oldValue?: string | null;
  newValue?: string | null;
  createdAt: string;
}

