import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiAuthResponse,
  AuthSession,
  AuthUser,
  LoginRequest,
  normalizeRole,
  UserRole,
} from '../models/auth.models';

const SESSION_KEY = 'sms.auth.session';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const EMAIL_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sessionState = signal<AuthSession | null>(this.restoreSession());

  readonly session = this.sessionState.asReadonly();
  readonly user = computed(() => this.sessionState()?.user ?? null);
  readonly token = computed(() => this.sessionState()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => Boolean(this.sessionState()?.accessToken));

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http.post<ApiAuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      map((response) => this.toSession(response)),
      tap((session) => {
        this.sessionState.set(session);
        this.storage?.setItem(SESSION_KEY, JSON.stringify(session));
      }),
    );
  }

  logout(redirect = true): void {
    this.storage?.removeItem(SESSION_KEY);
    this.sessionState.set(null);
    if (redirect) void this.router.navigateByUrl('/login');
  }

  hasAnyRole(...roles: UserRole[]): boolean {
    const role = this.user()?.role;
    return Boolean(role && roles.includes(role));
  }

  private toSession(response: ApiAuthResponse): AuthSession {
    const accessToken = response.accessToken ?? response.token ?? response.jwtToken;
    if (!accessToken)
      throw new Error('The authentication response did not include an access token.');

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
      refreshToken: response.refreshToken,
      expiresAt: response.expiresAt,
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
      const claims = this.decodeClaims(session.accessToken);
      const expiresAt = Number(claims['exp'] ?? 0);
      if (expiresAt && expiresAt * 1000 <= Date.now()) {
        this.storage?.removeItem(SESSION_KEY);
        return null;
      }
      return session;
    } catch {
      this.storage?.removeItem(SESSION_KEY);
      return null;
    }
  }

  private get storage(): Storage | null {
    try {
      return typeof localStorage === 'undefined' ? null : localStorage;
    } catch {
      return null;
    }
  }
}
