import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { authInterceptor } from './auth.interceptor';

function tokenWith(claims: Record<string, unknown>): string {
  const payload = btoa(JSON.stringify(claims))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
  return `header.${payload}.signature`;
}

describe('authInterceptor', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  it('refreshes a failed API request once and retries it with the rotated access token', () => {
    const staleToken = tokenWith({
      sub: 'user-1',
      role: 'SupportAgent',
      email: 'agent@example.test',
      jti: 'stale',
    });
    const renewedToken = tokenWith({
      sub: 'user-1',
      role: 'SupportAgent',
      email: 'agent@example.test',
      jti: 'renewed',
    });

    localStorage.setItem(
      'sms.auth.session',
      JSON.stringify({
        accessToken: staleToken,
        refreshToken: 'refresh-1',
        refreshTokenExpiresAt: '2099-01-08T00:00:00Z',
        user: {
          id: 'user-1',
          firstName: 'Alex',
          lastName: 'Morgan',
          email: 'agent@example.test',
          role: 'SupportAgent',
        },
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);
    let responseBody: unknown;

    client.get(`${environment.apiUrl}/tickets`).subscribe((response) => {
      responseBody = response;
    });

    const initialRequest = http.expectOne(`${environment.apiUrl}/tickets`);
    expect(initialRequest.request.headers.get('Authorization')).toBe(`Bearer ${staleToken}`);
    initialRequest.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const refreshRequest = http.expectOne(`${environment.apiUrl}/auth/refresh`);
    expect(refreshRequest.request.body).toEqual({ refreshToken: 'refresh-1' });
    expect(refreshRequest.request.headers.has('Authorization')).toBe(false);
    refreshRequest.flush({
      accessToken: renewedToken,
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

    const retriedRequest = http.expectOne(`${environment.apiUrl}/tickets`);
    expect(retriedRequest.request.headers.get('Authorization')).toBe(`Bearer ${renewedToken}`);
    retriedRequest.flush([{ number: 1 }]);

    expect(responseBody).toEqual([{ number: 1 }]);
    expect(JSON.parse(localStorage.getItem('sms.auth.session') ?? '{}').refreshToken).toBe(
      'refresh-2',
    );
    http.verify();
  });
});