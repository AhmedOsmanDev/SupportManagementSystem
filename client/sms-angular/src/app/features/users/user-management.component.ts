import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { CreateUserRequest, ManagedUser, UserRole } from '../../core/models';
import { AdminService } from '../../core/services/admin.service';
import { apiErrorMessage } from '../../core/services/api-error';

@Component({
  selector: 'app-user-management',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
  ],
  template: `
    <main class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Administration</p>
          <h1>People & access</h1>
          <p>Create team accounts and control access to the workspace.</p>
        </div>
        <button
          mat-flat-button
          color="primary"
          class="primary-action"
          type="button"
          (click)="showCreate.set(!showCreate())"
        >
          {{ showCreate() ? 'Cancel' : '＋ Add user' }}
        </button>
      </header>

      @if (showCreate()) {
        <section class="panel create-user-panel">
          <div class="panel__header">
            <h2>Create an account</h2>
            <span class="subtle"
              >A temporary password can be changed outside this assessment flow.</span
            >
          </div>
          <div class="panel__body">
            <form [formGroup]="form" (ngSubmit)="createUser()" class="form-grid">
              <mat-form-field appearance="outline"
                ><mat-label>First name</mat-label
                ><input matInput formControlName="firstName" /><mat-error
                  >First name is required</mat-error
                ></mat-form-field
              >
              <mat-form-field appearance="outline"
                ><mat-label>Last name</mat-label
                ><input matInput formControlName="lastName" /><mat-error
                  >Last name is required</mat-error
                ></mat-form-field
              >
              <mat-form-field appearance="outline"
                ><mat-label>Email address</mat-label
                ><input matInput type="email" formControlName="email" autocomplete="off" />
                @if (form.controls.email.hasError('email')) {
                  <mat-error>Enter a valid email</mat-error>
                }
              </mat-form-field>
              <mat-form-field appearance="outline"
                ><mat-label>Role</mat-label
                ><mat-select formControlName="role">
                  @for (role of roles; track role) {
                    <mat-option [value]="role">{{ roleLabel(role) }}</mat-option>
                  }
                </mat-select></mat-form-field
              >
              <mat-form-field appearance="outline" class="span-2"
                ><mat-label>Temporary password</mat-label
                ><input
                  matInput
                  type="password"
                  formControlName="password"
                  autocomplete="new-password"
                /><mat-hint>At least 8 characters</mat-hint>
                @if (form.controls.password.hasError('minlength')) {
                  <mat-error>Use at least 8 characters</mat-error>
                }
              </mat-form-field>
              @if (formError()) {
                <div class="alert alert--error span-2">{{ formError() }}</div>
              }
              <div class="form-actions span-2">
                <button mat-flat-button color="primary" type="submit" [disabled]="saving()">
                  @if (saving()) {
                    <mat-spinner diameter="20" />
                  } @else {
                    Create account
                  }
                </button>
              </div>
            </form>
          </div>
        </section>
      }

      <section class="panel">
        <div class="panel__header">
          <div>
            <h2>Workspace users</h2>
            <span class="subtle">{{ users().length }} accounts</span>
          </div>
        </div>
        @if (loading()) {
          <div class="loading-state"><mat-spinner diameter="36" /></div>
        } @else if (error()) {
          <div class="error-state">
            <strong>Users couldn’t be loaded</strong>
            <p>{{ error() }}</p>
            <button mat-stroked-button (click)="load()">Try again</button>
          </div>
        } @else {
          <div class="table-wrap desktop-table">
            <table mat-table [dataSource]="users()" class="data-table">
              <ng-container matColumnDef="name"
                ><th mat-header-cell *matHeaderCellDef>User</th>
                <td mat-cell *matCellDef="let user">
                  <div class="user-cell">
                    <span class="avatar">{{ initials(user) }}</span
                    ><span
                      ><strong>{{ user.firstName }} {{ user.lastName }}</strong
                      ><small>{{ user.email }}</small></span
                    >
                  </div>
                </td></ng-container
              >
              <ng-container matColumnDef="role"
                ><th mat-header-cell *matHeaderCellDef>Role</th>
                <td mat-cell *matCellDef="let user">
                  <span class="role-tag">{{ roleLabel(user.role) }}</span>
                </td></ng-container
              >
              <ng-container matColumnDef="created"
                ><th mat-header-cell *matHeaderCellDef>Joined</th>
                <td mat-cell *matCellDef="let user">
                  {{ user.createdAt ? (user.createdAt | date: 'mediumDate') : '—' }}
                </td></ng-container
              >
              <ng-container matColumnDef="status"
                ><th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let user">
                  <span class="status-dot" [class.status-dot--active]="user.isActive">{{
                    user.isActive ? 'Active' : 'Inactive'
                  }}</span>
                </td></ng-container
              >
              <ng-container matColumnDef="actions"
                ><th mat-header-cell *matHeaderCellDef>
                  <span class="visually-hidden">Actions</span>
                </th>
                <td mat-cell *matCellDef="let user" class="action-cell">
                  <button
                    mat-stroked-button
                    type="button"
                    [disabled]="saving()"
                    (click)="toggleActive(user)"
                  >
                    {{ user.isActive ? 'Deactivate' : 'Activate' }}
                  </button>
                </td></ng-container
              >
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          <div class="card-list user-card-list">
            @for (user of users(); track user.id) {
              <article class="user-card">
                <div class="user-cell">
                  <span class="avatar">{{ initials(user) }}</span
                  ><span
                    ><strong>{{ user.firstName }} {{ user.lastName }}</strong
                    ><small>{{ user.email }}</small></span
                  >
                </div>
                <div class="user-card__footer">
                  <span class="role-tag">{{ roleLabel(user.role) }}</span
                  ><button mat-button type="button" (click)="toggleActive(user)">
                    {{ user.isActive ? 'Deactivate' : 'Activate' }}
                  </button>
                </div>
              </article>
            }
          </div>
        }
      </section>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagementComponent {
  private readonly admin = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly users = signal<ManagedUser[]>([]);
  readonly roles: UserRole[] = ['Admin', 'SupportAgent', 'Customer'];
  readonly columns = ['name', 'role', 'created', 'status', 'actions'];
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly showCreate = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly form = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(80)],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(80)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8)],
    }),
    role: new FormControl<UserRole>('SupportAgent', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.admin
      .getUsers()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (users) => this.users.set(users),
        error: (error) => this.error.set(apiErrorMessage(error, 'Please try again.')),
      });
  }

  createUser(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.formError.set('');
    this.admin
      .createUser(this.form.getRawValue() as CreateUserRequest)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.snackBar.open('Account created', 'Dismiss', { duration: 2800 });
          this.form.reset({
            firstName: '',
            lastName: '',
            email: '',
            password: '',
            role: 'SupportAgent',
          });
          this.showCreate.set(false);
          this.load();
        },
        error: (error) =>
          this.formError.set(apiErrorMessage(error, 'The account could not be created.')),
      });
  }

  toggleActive(user: ManagedUser): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.admin
      .setUserActive(user.id, !user.isActive)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.users.update((users) =>
            users.map((item) =>
              item.id === user.id ? { ...item, isActive: !item.isActive } : item,
            ),
          );
          this.snackBar.open(`Account ${user.isActive ? 'deactivated' : 'activated'}`, 'Dismiss', {
            duration: 2500,
          });
        },
        error: (error) =>
          this.snackBar.open(
            apiErrorMessage(error, 'The account could not be updated.'),
            'Dismiss',
            { duration: 4500 },
          ),
      });
  }

  initials(user: ManagedUser): string {
    return `${user.firstName[0] ?? ''}${user.lastName[0] ?? ''}`.toUpperCase();
  }
  roleLabel(role: UserRole): string {
    return role === 'SupportAgent' ? 'Support agent' : role;
  }
}
