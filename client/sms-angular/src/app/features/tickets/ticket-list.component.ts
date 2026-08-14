import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, startWith } from 'rxjs';
import {
  ticketPriorities,
  ticketStatuses,
  TicketSummary,
  displayStatus,
} from '../../core/models';
import { apiErrorMessage } from '../../core/services/api-error';
import { AuthService } from '../../core/services/auth.service';
import { TicketService } from '../../core/services/ticket.service';

@Component({
  selector: 'app-ticket-list',
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
  ],
  template: `
    <main class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Ticket workspace</p>
          <h1>{{ heading() }}</h1>
          <p>{{ subtitle() }}</p>
        </div>
        @if (auth.hasAnyRole('Customer')) {
          <a mat-flat-button color="primary" class="primary-action" routerLink="/tickets/new"
            >＋ Create ticket</a
          >
        }
      </header>

      <section class="panel" aria-label="Tickets">
        <form class="filters" [formGroup]="filters" aria-label="Filter tickets">
          <mat-form-field appearance="outline">
            <mat-label>Search tickets</mat-label>
            <input matInput formControlName="search" placeholder="Title, description, or number" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              <mat-option value="">All statuses</mat-option>
              @for (status of statuses; track status) {
                <mat-option [value]="status">{{ statusLabel(status) }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Priority</mat-label>
            <mat-select formControlName="priority">
              <mat-option value="">All priorities</mat-option>
              @for (priority of priorities; track priority) {
                <mat-option [value]="priority">{{ priority }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </form>

        <div class="result-meta">
          <span>{{ totalCount() }} {{ totalCount() === 1 ? 'ticket' : 'tickets' }}</span>
          @if (loading()) {
            <span>Refreshing…</span>
          }
        </div>

        @if (loading() && tickets().length === 0) {
          <div class="loading-state">
            <mat-spinner diameter="36" /><span class="subtle">Loading tickets</span>
          </div>
        } @else if (error()) {
          <div class="error-state">
            <strong>Tickets couldn’t be loaded</strong>
            <p>{{ error() }}</p>
            <button mat-stroked-button type="button" (click)="loadTickets()">Try again</button>
          </div>
        } @else if (tickets().length === 0) {
          <div class="empty-state">
            <strong>No matching tickets</strong>
            <p>
              Try adjusting the filters{{
                auth.hasAnyRole('Customer') ? ', or create a new support request.' : '.'
              }}
            </p>
            @if (auth.hasAnyRole('Customer')) {
              <a mat-stroked-button routerLink="/tickets/new">Create ticket</a>
            }
          </div>
        } @else {
          <div class="table-wrap desktop-table">
            <table
              mat-table
              matSort
              [dataSource]="tickets()"
              [matSortActive]="sortBy()"
              [matSortDirection]="sortDirection()"
              (matSortChange)="sortChanged($event)"
              class="data-table"
            >
              <ng-container matColumnDef="number">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Ticket</th>
                <td mat-cell *matCellDef="let ticket">
                  <span class="ticket-number">{{ ticket.number }}</span>
                </td>
              </ng-container>
              <ng-container matColumnDef="title">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Subject</th>
                <td mat-cell *matCellDef="let ticket" class="ticket-cell">
                  <strong>{{ ticket.title }}</strong
                  ><small>{{ ticket.customerName || 'Customer request' }}</small>
                </td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
                <td mat-cell *matCellDef="let ticket">
                  <span class="badge badge--{{ ticket.status }}">{{
                    statusLabel(ticket.status)
                  }}</span>
                </td>
              </ng-container>
              <ng-container matColumnDef="priority">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Priority</th>
                <td mat-cell *matCellDef="let ticket">
                  <span class="priority priority--{{ ticket.priority }}">{{
                    ticket.priority
                  }}</span>
                </td>
              </ng-container>
              <ng-container matColumnDef="agent">
                <th mat-header-cell *matHeaderCellDef>Owner</th>
                <td mat-cell *matCellDef="let ticket">
                  {{ ticket.assignedAgentName || 'Unassigned' }}
                </td>
              </ng-container>
              <ng-container matColumnDef="createdAt">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Created</th>
                <td mat-cell *matCellDef="let ticket">{{ ticket.createdAt | date: 'MMM d, y' }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr
                mat-row
                *matRowDef="let row; columns: columns"
                [routerLink]="['/tickets', row.number]"
                [attr.aria-label]="'Open ticket ' + row.number"
              ></tr>
            </table>
          </div>

          <div class="card-list">
            @for (ticket of tickets(); track ticket.number) {
              <a class="ticket-card" [routerLink]="['/tickets', ticket.number]">
                <div class="ticket-card__top">
                  <span class="ticket-number">{{ ticket.number }}</span
                  ><span class="badge badge--{{ ticket.status }}">{{
                    statusLabel(ticket.status)
                  }}</span>
                </div>
                <h3>{{ ticket.title }}</h3>
                <div class="ticket-card__meta">
                  <span class="priority priority--{{ ticket.priority }}">{{ ticket.priority }}</span
                  ><span>{{ ticket.createdAt | date: 'MMM d' }}</span>
                </div>
              </a>
            }
          </div>
        }

        @if (totalCount() > 0) {
          <mat-paginator
            [length]="totalCount()"
            [pageIndex]="pageIndex()"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="pageChanged($event)"
            aria-label="Ticket pages"
          />
        }
      </section>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketListComponent {
  readonly auth = inject(AuthService);
  private readonly ticketsService = inject(TicketService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statuses = ticketStatuses;
  readonly priorities = ticketPriorities;
  readonly columns = ['number', 'title', 'status', 'priority', 'agent', 'createdAt'];
  readonly tickets = signal<TicketSummary[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly sortBy = signal('createdAt');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly heading = signal(
    this.auth.hasAnyRole('SupportAgent')
      ? 'Assigned tickets'
      : this.auth.hasAnyRole('Customer')
        ? 'Your support requests'
        : 'All tickets',
  );
  readonly subtitle = signal(
    this.auth.hasAnyRole('Customer')
      ? 'Track progress and continue the conversation with support.'
      : 'Review, prioritize, and move requests toward resolution.',
  );
  readonly filters = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    status: new FormControl('', { nonNullable: true }),
    priority: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    this.filters.valueChanges
      .pipe(
        startWith(this.filters.getRawValue()),
        debounceTime(250),
        distinctUntilChanged(
          (previous, current) => JSON.stringify(previous) === JSON.stringify(current),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadTickets();
      });
  }

  loadTickets(): void {
    this.loading.set(true);
    this.error.set('');
    const filters = this.filters.getRawValue();
    this.ticketsService
      .getTickets({
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        search: filters.search.trim(),
        status: filters.status as never,
        priority: filters.priority as never,
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.tickets.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error) => this.error.set(apiErrorMessage(error, 'Please try again.')),
      });
  }

  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadTickets();
  }

  sortChanged(sort: Sort): void {
    this.sortBy.set(sort.active || 'createdAt');
    this.sortDirection.set(sort.direction || 'desc');
    this.pageIndex.set(0);
    this.loadTickets();
  }

  statusLabel(status: string): string {
    return displayStatus(status);
  }
}
