import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

function tokenWith(claims: Record<string, unknown>): string {
  const payload = btoa(JSON.stringify(claims))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
  return `header.${payload}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('stores a role-aware session returned by login', () => {
    const accessToken = tokenWith({
      sub: 'user-1',
      role: 'SupportAgent',
      email: 'agent@example.test',
    });
    service.login({ email: 'agent@example.test', password: 'password' }).subscribe();

    const request = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(request.request.method).toBe('POST');
    request.flush({
      accessToken,
      refreshToken: 'refresh-1',
      expiresAt: '2099-01-01T00:00:00Z',
      refreshTokenExpiresAt: '2099-01-08T00:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Alex',
        lastName: 'Morgan',
        email: 'agent@example.test',
        role: 'SupportAgent',
      },
    });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasAnyRole('SupportAgent')).toBe(true);
    const session = JSON.parse(localStorage.getItem('sms.auth.session') ?? '{}');
    expect(session.accessToken).toBe(accessToken);
    expect(session.refreshToken).toBe('refresh-1');
  });

  it('replaces the stored tokens when a refresh succeeds', () => {
    const accessToken = tokenWith({
      sub: 'user-1',
      role: 'SupportAgent',
      email: 'agent@example.test',
    });
    const rotatedToken = tokenWith({
      sub: 'user-1',
      role: 'SupportAgent',
      email: 'agent@example.test',
      jti: 'rotated',
    });

    service.login({ email: 'agent@example.test', password: 'password' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush({
      accessToken,
      refreshToken: 'refresh-1',
      refreshTokenExpiresAt: '2099-01-08T00:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Alex',
        lastName: 'Morgan',
        email: 'agent@example.test',
        role: 'SupportAgent',
      },
    });

    let refreshedToken = '';
    service.refreshAccessToken().subscribe((token) => {
      refreshedToken = token;
    });

    const request = http.expectOne(`${environment.apiUrl}/auth/refresh`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush({
      accessToken: rotatedToken,
      refreshToken: 'refresh-2',
      refreshTokenExpiresAt: '2099-01-15T00:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Alex',
        lastName: 'Morgan',
        email: 'agent@example.test',
        role: 'SupportAgent',
      },
    });

    expect(refreshedToken).toBe(rotatedToken);
    const session = JSON.parse(localStorage.getItem('sms.auth.session') ?? '{}');
    expect(session.accessToken).toBe(rotatedToken);
    expect(session.refreshToken).toBe('refresh-2');
  });

  it('posts the current refresh token before clearing the local session on logout', () => {
    const accessToken = tokenWith({
      sub: 'user-1',
      role: 'Customer',
      email: 'customer@example.test',
    });

    service.login({ email: 'customer@example.test', password: 'password' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush({
      accessToken,
      refreshToken: 'refresh-1',
      refreshTokenExpiresAt: '2099-01-08T00:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Casey',
        lastName: 'Customer',
        email: 'customer@example.test',
        role: 'Customer',
      },
    });

    service.logout(false);

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('sms.auth.session')).toBeNull();

    const request = http.expectOne(`${environment.apiUrl}/auth/logout`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });
});