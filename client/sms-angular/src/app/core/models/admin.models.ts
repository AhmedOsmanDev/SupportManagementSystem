import { UserRole } from './auth.models';

export interface ManagedUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt?: string;
  assignedTicketCount?: number;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: UserRole;
}

export interface AgentWorkload {
  agentId: string;
  agentName: string;
  activeTickets: number;
  totalMinutes: number;
}

export interface DashboardMetrics {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  openCriticalTickets: number;
  averageResolutionHours: number;
  agentWorkload: AgentWorkload[];
}
