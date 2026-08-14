export interface TimeEntry {
  id: string;
  agentId?: string;
  agentName: string;
  workDate: string;
  durationMinutes: number;
  description: string;
  createdAt?: string;
}

