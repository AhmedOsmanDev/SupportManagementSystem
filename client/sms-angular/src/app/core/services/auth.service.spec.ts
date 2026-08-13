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
    expect(JSON.parse(localStorage.getItem('sms.auth.session') ?? '{}').accessToken).toBe(
      accessToken,
    );
  });

  it('clears the local session on logout', () => {
    localStorage.setItem(
      'sms.auth.session',
      JSON.stringify({ accessToken: 'token', user: { role: 'Customer' } }),
    );
    service.logout(false);
    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('sms.auth.session')).toBeNull();
  });
});
