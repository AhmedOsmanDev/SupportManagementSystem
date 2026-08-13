import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatListModule,
    MatDividerModule,
  ],
  template: `
    <mat-sidenav-container class="app-shell">
      <mat-sidenav
        #drawer
        class="app-nav"
        [mode]="isHandset() ? 'over' : 'side'"
        [opened]="!isHandset()"
      >
        <a class="nav-brand" routerLink="/tickets" (click)="closeOnHandset(drawer)">
          <span class="brand-mark" aria-hidden="true">S</span>
          <span><strong>Supportly</strong><small>Service workspace</small></span>
        </a>

        <nav aria-label="Main navigation">
          <p class="nav-label">Workspace</p>
          <mat-nav-list>
            @if (auth.hasAnyRole('Admin')) {
              <a
                mat-list-item
                routerLink="/dashboard"
                routerLinkActive="nav-active"
                (click)="closeOnHandset(drawer)"
              >
                <span class="nav-symbol" aria-hidden="true">▦</span><span>Dashboard</span>
              </a>
            }
            <a
              mat-list-item
              routerLink="/tickets"
              routerLinkActive="nav-active"
              [routerLinkActiveOptions]="{ exact: true }"
              (click)="closeOnHandset(drawer)"
            >
              <span class="nav-symbol" aria-hidden="true">◇</span><span>Tickets</span>
            </a>
            @if (auth.hasAnyRole('Customer')) {
              <a
                mat-list-item
                routerLink="/tickets/new"
                routerLinkActive="nav-active"
                (click)="closeOnHandset(drawer)"
              >
                <span class="nav-symbol" aria-hidden="true">＋</span><span>New ticket</span>
              </a>
            }
            @if (auth.hasAnyRole('Admin')) {
              <a
                mat-list-item
                routerLink="/users"
                routerLinkActive="nav-active"
                (click)="closeOnHandset(drawer)"
              >
                <span class="nav-symbol" aria-hidden="true">◎</span><span>People</span>
              </a>
            }
          </mat-nav-list>
        </nav>

        <div class="nav-account">
          <mat-divider />
          <div class="account-summary">
            <span class="avatar">{{ initials() }}</span>
            <span class="account-copy"
              ><strong>{{ fullName() }}</strong
              ><small>{{ auth.user()?.role }}</small></span
            >
          </div>
          <button mat-button type="button" class="sign-out" (click)="auth.logout()">
            Sign out
          </button>
        </div>
      </mat-sidenav>

      <mat-sidenav-content>
        @if (isHandset()) {
          <mat-toolbar class="mobile-toolbar">
            <button mat-button type="button" aria-label="Open navigation" (click)="drawer.toggle()">
              ☰
            </button>
            <span>{{ currentSection() }}</span>
            <span class="toolbar-spacer"></span>
            <span class="avatar avatar--small">{{ initials() }}</span>
          </mat-toolbar>
        }
        <div class="page-host"><router-outlet /></div>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly isHandset = signal(false);
  readonly currentSection = signal('Tickets');
  readonly fullName = computed(() => {
    const user = this.auth.user();
    return [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email || 'Account';
  });
  readonly initials = computed(() => {
    const user = this.auth.user();
    return `${user?.firstName?.[0] ?? ''}${user?.lastName?.[0] ?? ''}`.toUpperCase() || 'U';
  });

  constructor() {
    inject(BreakpointObserver)
      .observe('(max-width: 899px)')
      .pipe(
        map((result) => result.matches),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((matches) => this.isHandset.set(matches));

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => {
        const segment = event.urlAfterRedirects.split('?')[0].split('/')[1] || 'tickets';
        this.currentSection.set(segment.charAt(0).toUpperCase() + segment.slice(1));
      });
  }

  closeOnHandset(drawer: MatSidenav): void {
    if (this.isHandset()) void drawer.close();
  }
}
