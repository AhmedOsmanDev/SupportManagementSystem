import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { apiErrorMessage } from '../../core/services/api-error';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <main class="login-page">
      <section class="login-brand" aria-label="Support management introduction">
        <div class="brand-mark brand-mark--large" aria-hidden="true">S</div>
        <p class="eyebrow">Support workspace</p>
        <h1>Turn every request into a clear next step.</h1>
        <p class="login-intro">
          One focused place for customers and support teams to resolve issues, share context, and
          keep work moving.
        </p>
        <div class="login-proof">
          <span><strong>Fast</strong> triage</span>
          <span><strong>Clear</strong> ownership</span>
          <span><strong>Secure</strong> access</span>
        </div>
      </section>

      <section class="login-panel">
        <mat-card appearance="outlined" class="login-card">
          <mat-card-header>
            <mat-card-title>Welcome back</mat-card-title>
            <mat-card-subtitle>Sign in to your support workspace</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="form" (ngSubmit)="submit()" class="stack-form" novalidate>
              <mat-form-field appearance="outline">
                <mat-label>Email address</mat-label>
                <input matInput type="email" formControlName="email" autocomplete="username" />
                @if (form.controls.email.hasError('required')) {
                  <mat-error>Email is required</mat-error>
                }
                @if (form.controls.email.hasError('email')) {
                  <mat-error>Enter a valid email address</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Password</mat-label>
                <input
                  matInput
                  type="password"
                  formControlName="password"
                  autocomplete="current-password"
                />
                @if (form.controls.password.hasError('required')) {
                  <mat-error>Password is required</mat-error>
                }
              </mat-form-field>

              @if (error()) {
                <div class="alert alert--error" role="alert">{{ error() }}</div>
              }

              <button
                mat-flat-button
                color="primary"
                class="primary-action"
                type="submit"
                [disabled]="loading()"
              >
                @if (loading()) {
                  <mat-spinner diameter="20" />
                } @else {
                  Sign in
                }
              </button>
            </form>
          </mat-card-content>
        </mat-card>
        <p class="security-note">Protected by role-based access and encrypted authentication.</p>
      </section>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    if (this.auth.isAuthenticated()) void this.router.navigateByUrl('/tickets');
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.auth
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          const destination =
            returnUrl && returnUrl.startsWith('/')
              ? returnUrl
              : this.auth.hasAnyRole('Admin')
                ? '/dashboard'
                : '/tickets';
          void this.router.navigateByUrl(destination);
        },
        error: (error) =>
          this.error.set(apiErrorMessage(error, 'Sign-in failed. Check your email and password.')),
      });
  }
}
