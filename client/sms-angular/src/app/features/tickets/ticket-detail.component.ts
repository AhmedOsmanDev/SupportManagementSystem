import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import {
  ManagedUser,
  formatMinutes,
  allowedStatusTransitions,
  TicketDetail,
  TicketPriority,
  ticketPriorities,
  TicketStatus,
  displayStatus,
} from '../../core/models';
import { AdminService } from '../../core/services/admin.service';
import { apiErrorMessage } from '../../core/services/api-error';
import { AuthService } from '../../core/services/auth.service';
import { TicketService } from '../../core/services/ticket.service';

@Component({
  selector: 'app-ticket-detail',
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTabsModule,
  ],
  template: `
    <main class="page detail-page">
      <a class="back-link" routerLink="/tickets">← Back to tickets</a>

      @if (loading() && !ticket()) {
        <div class="loading-state">
          <mat-spinner diameter="40" /><span class="subtle">Loading ticket</span>
        </div>
      } @else if (error() && !ticket()) {
        <div class="panel error-state">
          <strong>Ticket couldn’t be loaded</strong>
          <p>{{ error() }}</p>
          <button mat-stroked-button (click)="load()">Try again</button>
        </div>
      } @else if (ticket(); as item) {
        <header class="detail-header">
          <div>
            <div class="detail-kicker">
              <span class="ticket-number" [attr.aria-label]="'Ticket number ' + item.number">{{
                item.number
              }}</span
              ><span class="badge badge--{{ item.status }}">{{ statusLabel(item.status) }}</span
              ><span class="priority priority--{{ item.priority }}">{{ item.priority }}</span>
            </div>
            <h1>{{ item.title }}</h1>
            <p>
              Opened {{ item.createdAt | date: 'mediumDate' }} by
              {{ item.customerName || 'Customer' }}
            </p>
          </div>
          @if (saving()) {
            <div class="saving-label"><mat-spinner diameter="18" /> Saving change…</div>
          }
        </header>

        @if (error()) {
          <div class="alert alert--error page-alert" role="alert">{{ error() }}</div>
        }

        <div class="detail-grid">
          <section class="panel detail-main">
            <mat-tab-group animationDuration="150ms">
              <mat-tab label="Overview">
                <div class="tab-content">
                  <h2>Issue description</h2>
                  <p class="ticket-description">{{ item.description }}</p>
                  <dl class="detail-facts">
                    <div>
                      <dt>Last updated</dt>
                      <dd>{{ item.updatedAt || item.createdAt | date: 'medium' }}</dd>
                    </div>
                    <div>
                      <dt>Total time logged</dt>
                      <dd>{{ minutes(item.totalTimeMinutes) }}</dd>
                    </div>
                    <div>
                      <dt>Resolution</dt>
                      <dd>
                        {{
                          item.resolvedAt ? (item.resolvedAt | date: 'mediumDate') : 'Not resolved'
                        }}
                      </dd>
                    </div>
                  </dl>
                </div>
              </mat-tab>

              <mat-tab [label]="'Comments (' + item.comments.length + ')'">
                <div class="tab-content">
                  <div class="comment-list">
                    @for (comment of item.comments; track comment.id) {
                      <article class="comment">
                        <div class="comment-avatar">
                          {{ comment.authorName.charAt(0).toUpperCase() }}
                        </div>
                        <div class="comment-body">
                          <header>
                            <strong>{{ comment.authorName }}</strong
                            ><span>{{ comment.authorRole }}</span
                            ><time>{{ comment.createdAt | date: 'medium' }}</time>
                          </header>
                          <p>{{ comment.content }}</p>
                        </div>
                      </article>
                    } @empty {
                      <div class="empty-inline">No comments yet. Start the conversation below.</div>
                    }
                  </div>

                  @if (item.status !== 'Closed') {
                    <form class="comment-form" [formGroup]="commentForm" (ngSubmit)="addComment()">
                      <mat-form-field appearance="outline">
                        <mat-label>Add a comment</mat-label>
                        <textarea
                          matInput
                          formControlName="content"
                          rows="4"
                          maxlength="2000"
                          placeholder="Share an update or ask a question"
                        ></textarea>
                        @if (commentForm.controls.content.hasError('required')) {
                          <mat-error>Write a comment first</mat-error>
                        }
                      </mat-form-field>
                      <div class="form-actions">
                        <button mat-flat-button color="primary" type="submit" [disabled]="saving()">
                          Post comment
                        </button>
                      </div>
                    </form>
                  } @else {
                    <div class="alert alert--info">
                      This ticket is closed, so new comments are disabled.
                    </div>
                  }
                </div>
              </mat-tab>

              <mat-tab [label]="'Activity (' + item.activities.length + ')'">
                <div class="tab-content">
                  <ol class="timeline">
                    @for (activity of item.activities; track activity.id) {
                      <li>
                        <span class="timeline-dot" aria-hidden="true"></span>
                        <div>
                          <strong>{{ activity.description || statusLabel(activity.type) }}</strong>
                          @if (activity.oldValue || activity.newValue) {
                            <p>{{ activity.oldValue || '—' }} → {{ activity.newValue || '—' }}</p>
                          }
                          <time
                            >{{ activity.createdAt | date: 'medium'
                            }}{{ activity.performedBy ? ' · ' + activity.performedBy : '' }}</time
                          >
                        </div>
                      </li>
                    } @empty {
                      <li class="empty-inline">No recorded activity yet.</li>
                    }
                  </ol>
                </div>
              </mat-tab>

              <mat-tab [label]="'Time (' + item.timeEntries.length + ')'">
                <div class="tab-content">
                  <div class="time-total">
                    <span>Total logged</span><strong>{{ minutes(item.totalTimeMinutes) }}</strong>
                  </div>
                  <div class="time-list">
                    @for (entry of item.timeEntries; track entry.id) {
                      <article>
                        <div>
                          <strong>{{ entry.description }}</strong
                          ><span
                            >{{ entry.agentName }} · {{ entry.workDate | date: 'mediumDate' }}</span
                          >
                        </div>
                        <b>{{ minutes(entry.durationMinutes) }}</b>
                      </article>
                    } @empty {
                      <div class="empty-inline">No time has been logged on this ticket.</div>
                    }
                  </div>

                  @if (auth.hasAnyRole('SupportAgent') && item.status !== 'Closed') {
                    <form class="time-form" [formGroup]="timeForm" (ngSubmit)="logTime()">
                      <h3>Log work</h3>
                      <div class="form-grid">
                        <mat-form-field appearance="outline"
                          ><mat-label>Work date</mat-label
                          ><input matInput type="date" formControlName="workDate"
                        /></mat-form-field>
                        <mat-form-field appearance="outline"
                          ><mat-label>Duration (minutes)</mat-label
                          ><input
                            matInput
                            type="number"
                            min="1"
                            max="1440"
                            formControlName="durationMinutes"
                        /></mat-form-field>
                        <mat-form-field appearance="outline" class="span-2"
                          ><mat-label>Work performed</mat-label
                          ><textarea
                            matInput
                            rows="3"
                            formControlName="description"
                            maxlength="1000"
                          ></textarea>
                        </mat-form-field>
                      </div>
                      <div class="form-actions">
                        <button mat-flat-button color="primary" type="submit" [disabled]="saving()">
                          Log time
                        </button>
                      </div>
                    </form>
                  }
                </div>
              </mat-tab>
            </mat-tab-group>
          </section>

          <aside class="detail-sidebar">
            <section class="panel">
              <div class="panel__header"><h2>Ticket controls</h2></div>
              <div class="panel__body control-stack">
                @if (availableStatuses(item.status).length) {
                  <mat-form-field appearance="outline">
                    <mat-label>Change status</mat-label>
                    <mat-select [value]="item.status" (selectionChange)="changeStatus($event)">
                      <mat-option [value]="item.status">{{ statusLabel(item.status) }}</mat-option>
                      @for (status of availableStatuses(item.status); track status) {
                        <mat-option [value]="status">{{ statusLabel(status) }}</mat-option>
                      }
                    </mat-select>
                  </mat-form-field>
                } @else {
                  <div class="control-row">
                    <span>Status</span
                    ><span class="badge badge--{{ item.status }}">{{
                      statusLabel(item.status)
                    }}</span>
                  </div>
                }

                @if (auth.hasAnyRole('Admin')) {
                  <mat-form-field appearance="outline">
                    <mat-label>Priority</mat-label>
                    <mat-select [value]="item.priority" (selectionChange)="changePriority($event)">
                      @for (priority of priorities; track priority) {
                        <mat-option [value]="priority">{{ priority }}</mat-option>
                      }
                    </mat-select>
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>Assigned agent</mat-label>
                    <mat-select
                      [value]="item.assignedAgentId || ''"
                      (selectionChange)="changeAssignment($event)"
                    >
                      <mat-option value="">Unassigned</mat-option>
                      @for (agent of agents(); track agent.id) {
                        <mat-option [value]="agent.id"
                          >{{ agent.firstName }} {{ agent.lastName }}</mat-option
                        >
                      }
                    </mat-select>
                  </mat-form-field>
                }

                <div class="sidebar-meta">
                  <div>
                    <span>Customer</span><strong>{{ item.customerName || 'Customer' }}</strong>
                  </div>
                  <div>
                    <span>Owner</span><strong>{{ item.assignedAgentName || 'Unassigned' }}</strong>
                  </div>
                  <div>
                    <span>Created</span><strong>{{ item.createdAt | date: 'mediumDate' }}</strong>
                  </div>
                  <div>
                    <span>Time logged</span><strong>{{ minutes(item.totalTimeMinutes) }}</strong>
                  </div>
                </div>
              </div>
            </section>
          </aside>
        </div>
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketDetailComponent {
  readonly auth = inject(AuthService);
  private readonly tickets = inject(TicketService);
  private readonly admin = inject(AdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  readonly number = Number(this.route.snapshot.paramMap.get('number'));
  readonly ticket = signal<TicketDetail | null>(null);
  readonly agents = signal<ManagedUser[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly priorities = ticketPriorities;
  readonly commentForm = new FormGroup({
    content: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(2000)],
    }),
  });
  readonly timeForm = new FormGroup({
    workDate: new FormControl(new Date().toISOString().slice(0, 10), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    durationMinutes: new FormControl(30, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(1440)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(1000)],
    }),
  });

  constructor() {
    this.load();
    if (this.auth.hasAnyRole('Admin')) {
      this.admin.getAgents().subscribe({ next: (agents) => this.agents.set(agents) });
    }
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.tickets
      .getTicket(this.number)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (ticket) => this.ticket.set(ticket),
        error: (error) => this.error.set(apiErrorMessage(error, 'Please try again.')),
      });
  }

  addComment(): void {
    if (this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }
    this.perform(
      this.tickets.addComment(this.number, {
        content: this.commentForm.controls.content.value.trim(),
      }),
      'Comment posted',
      () => this.commentForm.reset({ content: '' }),
    );
  }

  logTime(): void {
    if (this.timeForm.invalid) {
      this.timeForm.markAllAsTouched();
      return;
    }
    this.perform(
      this.tickets.logTime(this.number, this.timeForm.getRawValue()),
      'Time entry added',
      () =>
        this.timeForm.reset({
          workDate: new Date().toISOString().slice(0, 10),
          durationMinutes: 30,
          description: '',
        }),
    );
  }

  changeStatus(event: MatSelectChange): void {
    const status = event.value as TicketStatus;
    if (status !== this.ticket()?.status)
      this.perform(this.tickets.updateStatus(this.number, status), 'Status updated');
  }

  changePriority(event: MatSelectChange): void {
    const priority = event.value as TicketPriority;
    if (priority !== this.ticket()?.priority)
      this.perform(this.tickets.updatePriority(this.number, priority), 'Priority updated');
  }

  changeAssignment(event: MatSelectChange): void {
    const agentId = String(event.value || '') || null;
    if (agentId !== this.ticket()?.assignedAgentId)
      this.perform(this.tickets.assign(this.number, agentId), 'Assignment updated');
  }

  availableStatuses(current: TicketStatus): TicketStatus[] {
    return allowedStatusTransitions(current, this.auth.user()?.role ?? 'Customer');
  }

  statusLabel(value: string): string {
    return displayStatus(value);
  }
  minutes(value = 0): string {
    return formatMinutes(value);
  }

  private perform(request: Observable<unknown>, success: string, afterSuccess?: () => void): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        afterSuccess?.();
        this.snackBar.open(success, 'Dismiss', { duration: 2800 });
        this.load();
      },
      error: (error) => this.error.set(apiErrorMessage(error, 'The change could not be saved.')),
    });
  }
}
