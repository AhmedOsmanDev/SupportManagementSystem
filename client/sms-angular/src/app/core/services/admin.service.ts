import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AgentWorkload,
  CreateUserRequest,
  DashboardMetrics,
  ManagedUser,
} from '../models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  getDashboard(): Observable<DashboardMetrics> {
    return this.http
      .get<Partial<DashboardMetrics> & { countsByStatus?: Record<string, number> }>(
        `${environment.apiUrl}/dashboard`,
      )
      .pipe(
        map((value) => {
          const counts = value.countsByStatus ?? {};
          const openTickets = Number(counts['Open'] ?? value.openTickets ?? 0);
          const inProgressTickets = Number(counts['InProgress'] ?? value.inProgressTickets ?? 0);
          const resolvedTickets = Number(counts['Resolved'] ?? value.resolvedTickets ?? 0);
          const closedTickets = Number(counts['Closed'] ?? value.closedTickets ?? 0);
          return {
            totalTickets: Number(
              value.totalTickets ??
                openTickets + inProgressTickets + resolvedTickets + closedTickets,
            ),
            openTickets,
            inProgressTickets,
            resolvedTickets,
            closedTickets,
            openCriticalTickets: Number(value.openCriticalTickets ?? 0),
            averageResolutionHours: Number(value.averageResolutionHours ?? 0),
            agentWorkload: (value.agentWorkload ?? []).map((agent) => ({
              agentId: String(agent.agentId),
              agentName: String(agent.agentName),
              activeTickets: Number(agent.activeTickets ?? 0),
              totalMinutes: Number(agent.totalMinutes ?? 0),
            })),
          };
        }),
      );
  }

  getUsers(): Observable<ManagedUser[]> {
    return this.http
      .get<ManagedUser[] | { items: ManagedUser[] }>(`${environment.apiUrl}/users`)
      .pipe(map((value) => (Array.isArray(value) ? value : (value.items ?? []))));
  }

  getAgents(): Observable<ManagedUser[]> {
    return this.http
      .get<ManagedUser[] | { items: ManagedUser[] }>(`${environment.apiUrl}/users`, {
        params: { role: 'SupportAgent', activeOnly: true },
      })
      .pipe(map((value) => (Array.isArray(value) ? value : (value.items ?? []))));
  }

  createUser(request: CreateUserRequest): Observable<ManagedUser> {
    return this.http.post<ManagedUser>(`${environment.apiUrl}/users`, request);
  }

  setUserActive(id: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/users/${encodeURIComponent(id)}/status`, {
      isActive,
    });
  }
}
