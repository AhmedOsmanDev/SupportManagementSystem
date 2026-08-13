import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddCommentRequest,
  CreateTicketRequest,
  LogTimeRequest,
  PagedResult,
  TicketDetail,
  TicketPriority,
  TicketQuery,
  TicketStatus,
  TicketSummary,
} from '../models/ticket.models';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/tickets`;

  getTickets(query: TicketQuery): Observable<PagedResult<TicketSummary>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);
    if (query.priority) params = params.set('priority', query.priority);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

    return this.http
      .get<unknown>(this.endpoint, { params })
      .pipe(map((value) => this.normalizePage(value, query)));
  }

  getTicket(number: string): Observable<TicketDetail> {
    return this.http
      .get<unknown>(`${this.endpoint}/${encodeURIComponent(number)}`)
      .pipe(map((value) => this.normalizeDetail(value)));
  }

  createTicket(request: CreateTicketRequest): Observable<TicketDetail> {
    return this.http
      .post<unknown>(this.endpoint, request)
      .pipe(map((value) => this.normalizeDetail(value)));
  }

  addComment(number: string, request: AddCommentRequest): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/${encodeURIComponent(number)}/comments`, request);
  }

  logTime(number: string, request: LogTimeRequest): Observable<void> {
    return this.http.post<void>(
      `${this.endpoint}/${encodeURIComponent(number)}/time-entries`,
      request,
    );
  }

  updateStatus(number: string, status: TicketStatus): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${encodeURIComponent(number)}/status`, {
      status,
    });
  }

  updatePriority(number: string, priority: TicketPriority): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${encodeURIComponent(number)}/priority`, {
      priority,
    });
  }

  assign(number: string, agentId: string | null): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${encodeURIComponent(number)}/assignment`, {
      agentId,
    });
  }

  private normalizePage(value: unknown, query: TicketQuery): PagedResult<TicketSummary> {
    const body = (value ?? {}) as Record<string, unknown>;
    const rawItems = Array.isArray(value)
      ? value
      : ((body['items'] ?? body['data'] ?? body['results'] ?? []) as unknown[]);
    const totalCount = Number(body['totalCount'] ?? body['count'] ?? rawItems.length);
    const pageSize = Number(body['pageSize'] ?? query.pageSize);
    return {
      items: rawItems.map((item) => this.normalizeSummary(item)),
      page: Number(body['page'] ?? body['pageNumber'] ?? query.page),
      pageSize,
      totalCount,
      totalPages: Number(body['totalPages'] ?? Math.ceil(totalCount / Math.max(pageSize, 1))),
    };
  }

  private normalizeSummary(value: unknown): TicketSummary {
    const item = (value ?? {}) as Record<string, unknown>;
    return {
      number: String(item['number'] ?? item['ticketNumber'] ?? item['id'] ?? ''),
      title: String(item['title'] ?? ''),
      description: String(item['description'] ?? ''),
      status: String(item['status'] ?? 'Open').replace(/\s/g, '') as TicketStatus,
      priority: String(item['priority'] ?? 'Medium') as TicketPriority,
      customerId: item['customerId'] ? String(item['customerId']) : undefined,
      customerName: item['customerName'] ? String(item['customerName']) : undefined,
      assignedAgentId: item['assignedAgentId'] ? String(item['assignedAgentId']) : null,
      assignedAgentName: item['assignedAgentName'] ? String(item['assignedAgentName']) : null,
      createdAt: String(item['createdAt'] ?? ''),
      updatedAt: item['updatedAt'] ? String(item['updatedAt']) : undefined,
      totalTimeMinutes: Number(item['totalTimeMinutes'] ?? item['totalMinutes'] ?? 0),
    };
  }

  private normalizeDetail(value: unknown): TicketDetail {
    const item = (value ?? {}) as Record<string, unknown>;
    const summary = this.normalizeSummary(value);
    return {
      ...summary,
      comments: ((item['comments'] ?? []) as Record<string, unknown>[]).map((comment) => ({
        id: String(comment['id'] ?? ''),
        content: String(comment['content'] ?? ''),
        authorId: comment['authorId'] ? String(comment['authorId']) : undefined,
        authorName: String(comment['authorName'] ?? comment['userName'] ?? 'User'),
        authorRole: comment['authorRole'] ? String(comment['authorRole']) : undefined,
        createdAt: String(comment['createdAt'] ?? ''),
      })),
      activities: ((item['activities'] ?? item['timeline'] ?? []) as Record<string, unknown>[]).map(
        (activity) => ({
          id: String(activity['id'] ?? ''),
          type: String(activity['type'] ?? activity['activityType'] ?? 'Change'),
          description: String(activity['description'] ?? ''),
          performedBy: activity['performedBy'] ? String(activity['performedBy']) : undefined,
          oldValue: activity['oldValue'] ? String(activity['oldValue']) : null,
          newValue: activity['newValue'] ? String(activity['newValue']) : null,
          createdAt: String(activity['createdAt'] ?? ''),
        }),
      ),
      timeEntries: ((item['timeEntries'] ?? []) as Record<string, unknown>[]).map((entry) => ({
        id: String(entry['id'] ?? ''),
        agentId: entry['agentId'] ? String(entry['agentId']) : undefined,
        agentName: String(entry['agentName'] ?? 'Support agent'),
        workDate: String(entry['workDate'] ?? ''),
        durationMinutes: Number(entry['durationMinutes'] ?? 0),
        description: String(entry['description'] ?? ''),
        createdAt: entry['createdAt'] ? String(entry['createdAt']) : undefined,
      })),
      resolvedAt: item['resolvedAt'] ? String(item['resolvedAt']) : null,
      closedAt: item['closedAt'] ? String(item['closedAt']) : null,
    };
  }
}
