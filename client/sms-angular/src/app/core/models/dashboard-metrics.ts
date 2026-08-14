import { AgentWorkload } from './agent-workload';

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

