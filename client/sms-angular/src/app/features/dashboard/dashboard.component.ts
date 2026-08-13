import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { DashboardMetrics } from '../../core/models/admin.models';
import { formatMinutes } from '../../core/models/ticket.models';
import { AdminService } from '../../core/services/admin.service';
import { apiErrorMessage } from '../../core/services/api-error';

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, RouterLink, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <main class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Operations overview</p>
          <h1>Support dashboard</h1>
          <p>A live view of ticket health and team workload.</p>
        </div>
        <a mat-stroked-button class="secondary-action" routerLink="/tickets">View all tickets</a>
      </header>

      @if (loading()) {
        <div class="loading-state">
          <mat-spinner diameter="40" /><span class="subtle">Building dashboard</span>
        </div>
      } @else if (error()) {
        <div class="panel error-state">
          <strong>Dashboard unavailable</strong>
          <p>{{ error() }}</p>
          <button mat-stroked-button (click)="load()">Try again</button>
        </div>
      } @else if (metrics(); as data) {
        <section class="metrics-grid" aria-label="Ticket metrics">
          <article class="metric-card">
            <span class="metric-card__label">All tickets</span
            ><strong class="metric-card__value">{{ data.totalTickets }}</strong
            ><span class="metric-card__hint">Across every status</span>
          </article>
          <article class="metric-card">
            <span class="metric-card__label">Active queue</span
            ><strong class="metric-card__value">{{
              data.openTickets + data.inProgressTickets
            }}</strong
            ><span class="metric-card__hint">Open and in progress</span>
          </article>
          <article class="metric-card metric-card--critical">
            <span class="metric-card__label">Open critical</span
            ><strong class="metric-card__value">{{ data.openCriticalTickets }}</strong
            ><span class="metric-card__hint">Needs immediate attention</span>
          </article>
          <article class="metric-card">
            <span class="metric-card__label">Avg. resolution</span
            ><strong class="metric-card__value"
              >{{ data.averageResolutionHours | number: '1.0-1' }}h</strong
            ><span class="metric-card__hint">From open to resolved</span>
          </article>
        </section>

        <div class="dashboard-grid">
          <section class="panel">
            <div class="panel__header">
              <div>
                <h2>Agent workload</h2>
                <span class="subtle">Active tickets by owner</span>
              </div>
              <a mat-button routerLink="/users">Manage team</a>
            </div>
            <div class="panel__body">
              <div
                class="bar-chart"
                role="img"
                aria-label="Bar chart showing active tickets per support agent"
              >
                @for (agent of data.agentWorkload; track agent.agentId) {
                  <div class="bar-row">
                    <span class="bar-label" [title]="agent.agentName">{{ agent.agentName }}</span>
                    <span class="bar-track"
                      ><span class="bar-fill" [style.width.%]="barWidth(agent.activeTickets)"></span
                    ></span>
                    <strong class="bar-value">{{ agent.activeTickets }}</strong>
                  </div>
                } @empty {
                  <div class="empty-inline">No agent workload data is available yet.</div>
                }
              </div>
              @if (data.agentWorkload.length) {
                <p class="chart-caption">
                  Team total: {{ totalActive(data) }} active tickets ·
                  {{ totalLogged(data) }} logged
                </p>
              }
            </div>
          </section>

          <section class="panel">
            <div class="panel__header"><h2>Status breakdown</h2></div>
            <div class="panel__body status-summary">
              <div class="status-row">
                <span class="badge badge--Open">Open</span><strong>{{ data.openTickets }}</strong>
              </div>
              <div class="status-row">
                <span class="badge badge--InProgress">In progress</span
                ><strong>{{ data.inProgressTickets }}</strong>
              </div>
              <div class="status-row">
                <span class="badge badge--Resolved">Resolved</span
                ><strong>{{ data.resolvedTickets }}</strong>
              </div>
              <div class="status-row">
                <span class="badge badge--Closed">Closed</span
                ><strong>{{ data.closedTickets }}</strong>
              </div>
            </div>
          </section>
        </div>
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private readonly admin = inject(AdminService);
  readonly metrics = signal<DashboardMetrics | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.admin
      .getDashboard()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (metrics) => this.metrics.set(metrics),
        error: (error) => this.error.set(apiErrorMessage(error, 'Please try again.')),
      });
  }

  barWidth(value: number): number {
    const maximum = Math.max(
      ...(this.metrics()?.agentWorkload.map((agent) => agent.activeTickets) ?? [1]),
      1,
    );
    return Math.max((value / maximum) * 100, value ? 4 : 0);
  }

  totalActive(data: DashboardMetrics): number {
    return data.agentWorkload.reduce((sum, agent) => sum + agent.activeTickets, 0);
  }
  totalLogged(data: DashboardMetrics): string {
    return formatMinutes(data.agentWorkload.reduce((sum, agent) => sum + agent.totalMinutes, 0));
  }
}
