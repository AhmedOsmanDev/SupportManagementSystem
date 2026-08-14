import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ticketPriorities, TicketPriority } from '../../core/models';
import { apiErrorMessage } from '../../core/services/api-error';
import { TicketService } from '../../core/services/ticket.service';

@Component({
  selector: 'app-ticket-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  template: `
    <main class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">New support request</p>
          <h1>How can we help?</h1>
          <p>Share enough context for the team to start investigating.</p>
        </div>
      </header>

      <section class="panel form-panel">
        <div class="panel__header">
          <h2>Ticket details</h2>
          <span class="subtle">All fields are required</span>
        </div>
        <div class="panel__body">
          <form [formGroup]="form" (ngSubmit)="submit()" class="form-grid" novalidate>
            <mat-form-field appearance="outline" class="span-2">
              <mat-label>Short summary</mat-label>
              <input
                matInput
                formControlName="title"
                maxlength="160"
                placeholder="e.g. Unable to export monthly report"
              />
              <mat-hint align="end">{{ form.controls.title.value.length }}/160</mat-hint>
              @if (form.controls.title.hasError('required')) {
                <mat-error>A title is required</mat-error>
              }
              @if (form.controls.title.hasError('minlength')) {
                <mat-error>Use at least 5 characters</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="span-2">
              <mat-label>Description</mat-label>
              <textarea
                matInput
                formControlName="description"
                rows="8"
                maxlength="4000"
                placeholder="What happened? What did you expect? Include steps to reproduce if possible."
              ></textarea>
              <mat-hint align="end">{{ form.controls.description.value.length }}/4000</mat-hint>
              @if (form.controls.description.hasError('required')) {
                <mat-error>A description is required</mat-error>
              }
              @if (form.controls.description.hasError('minlength')) {
                <mat-error>Use at least 10 characters</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Priority</mat-label>
              <mat-select formControlName="priority">
                @for (priority of priorities; track priority) {
                  <mat-option [value]="priority">{{ priority }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <div class="form-hint">Choose Critical only when business operations are blocked.</div>

            @if (error()) {
              <div class="alert alert--error span-2" role="alert">{{ error() }}</div>
            }

            <div class="form-actions span-2">
              <a mat-button routerLink="/tickets">Cancel</a>
              <button
                mat-flat-button
                color="primary"
                class="primary-action"
                type="submit"
                [disabled]="saving()"
              >
                @if (saving()) {
                  <mat-spinner diameter="20" />
                } @else {
                  Submit ticket
                }
              </button>
            </div>
          </form>
        </div>
      </section>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketCreateComponent {
  private readonly tickets = inject(TicketService);
  private readonly router = inject(Router);
  readonly priorities = ticketPriorities;
  readonly saving = signal(false);
  readonly error = signal('');
  readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5), Validators.maxLength(160)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10), Validators.maxLength(4000)],
    }),
    priority: new FormControl<TicketPriority>('Medium', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    this.tickets
      .createTicket({ ...value, title: value.title.trim(), description: value.description.trim() })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (ticket) => void this.router.navigate(['/tickets', ticket.number]),
        error: (error) =>
          this.error.set(apiErrorMessage(error, 'The ticket could not be created.')),
      });
  }
}
