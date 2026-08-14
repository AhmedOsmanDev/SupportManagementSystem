import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  EMPTY,
  catchError,
  finalize,
  map,
  Observable,
  shareReplay,
  tap,
  throwError,
} from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiAuthResponse,
  AuthSession,
  AuthUser,
  LoginRequest,
  normalizeRole,
  UserRole,
} from '../models';

const SESSION_KEY = 'sms.auth.session';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const EMAIL_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sessionState = signal<AuthSession | null>(this.restoreSession());
  private refreshRequest$: Observable<AuthSession> | null = null;

  readonly session = this.sessionState.asReadonly();
  readonly user = computed(() => this.sessionState()?.user ?? null);
  readonly token = computed(() => this.sessionState()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http.post<ApiAuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      map((response) => this.toSession(response)),
      tap((session) => this.persistSession(session)),
    );
  }

  refreshAccessToken(): Observable<string> {
    const refreshToken = this.sessionState()?.refreshToken?.trim();
    if (!refreshToken) {
      this.logout(true, false);
      return throwError(() => new Error('No refresh token is available.'));
    }

    if (!this.refreshRequest$) {
      this.refreshRequest$ = this.http
        .post<ApiAuthResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
        .pipe(
          map((response) => this.toSession(response)),
          tap((session) => {
            if (this.sessionState()?.refreshToken === refreshToken) {
              this.persistSession(session);
            }
          }),
          finalize(() => {
            this.refreshRequest$ = null;
          }),
          shareReplay(1),
        );
    }

    return this.refreshRequest$.pipe(map((session) => session.accessToken));
  }

  logout(redirect = true, revokeSessionOnServer = true): void {
    const refreshToken = this.sessionState()?.refreshToken?.trim();
    this.clearSession(redirect);

    if (revokeSessionOnServer && refreshToken) {
      this.http
        .post<void>(`${environment.apiUrl}/auth/logout`, { refreshToken })
        .pipe(catchError(() => EMPTY))
        .subscribe();
    }
  }

  hasAnyRole(...roles: UserRole[]): boolean {
    const role = this.user()?.role;
    return Boolean(role && roles.includes(role));
  }

  private toSession(response: ApiAuthResponse): AuthSession {
    const accessToken = response.accessToken ?? response.token ?? response.jwtToken;
    if (!accessToken)
      throw new Error('The authentication response did not include an access token.');

    const refreshToken = response.refreshToken?.trim();
    if (!refreshToken)
      throw new Error('The authentication response did not include a refresh token.');

    const claims = this.decodeClaims(accessToken);
    const suppliedUser = response.user ?? {};
    const fullName = String(claims['name'] ?? claims['unique_name'] ?? '')
      .trim()
      .split(/\s+/);
    const user: AuthUser = {
      id: String(suppliedUser.id ?? claims['sub'] ?? claims[ID_CLAIM] ?? ''),
      firstName: String(suppliedUser.firstName ?? claims['given_name'] ?? fullName[0] ?? ''),
      lastName: String(
        suppliedUser.lastName ?? claims['family_name'] ?? fullName.slice(1).join(' ') ?? '',
      ),
      email: String(suppliedUser.email ?? claims['email'] ?? claims[EMAIL_CLAIM] ?? ''),
      role: normalizeRole(suppliedUser.role ?? claims['role'] ?? claims[ROLE_CLAIM]),
    };

    return {
      accessToken,
      refreshToken,
      expiresAt: response.expiresAt,
      refreshTokenExpiresAt: response.refreshTokenExpiresAt,
      user,
    };
  }

  private decodeClaims(token: string): Record<string, unknown> {
    try {
      const segment = token.split('.')[1];
      if (!segment) return {};
      const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
      return JSON.parse(decodeURIComponent(escape(atob(base64)))) as Record<string, unknown>;
    } catch {
      return {};
    }
  }

  private restoreSession(): AuthSession | null {
    try {
      const raw = this.storage?.getItem(SESSION_KEY);
      if (!raw) return null;

      const session = JSON.parse(raw) as AuthSession;
      if (!session.accessToken || !session.refreshToken || this.isExpired(session.refreshTokenExpiresAt)) {
        this.storage?.removeItem(SESSION_KEY);
        return null;
      }

      return session;
    } catch {
      this.storage?.removeItem(SESSION_KEY);
      return null;
    }
  }

  private persistSession(session: AuthSession): void {
    this.sessionState.set(session);
    this.storage?.setItem(SESSION_KEY, JSON.stringify(session));
  }

  private clearSession(redirect: boolean): void {
    this.refreshRequest$ = null;
    this.storage?.removeItem(SESSION_KEY);
    this.sessionState.set(null);
    if (redirect) void this.router.navigateByUrl('/login');
  }

  private isExpired(value?: string): boolean {
    if (!value) return false;
    const time = Date.parse(value);
    return Number.isFinite(time) && time <= Date.now();
  }

  private get storage(): Storage | null {
    try {
      return typeof localStorage === 'undefined' ? null : localStorage;
    } catch {
      return null;
    }
  }
}