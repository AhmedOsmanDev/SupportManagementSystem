import {
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

const RETRIED_WITH_REFRESH = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const authorizedRequest = addAuthorizationHeader(request, auth.token());

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      if (!shouldRefreshRequest(error, request, auth)) {
        return throwError(() => error);
      }

      return auth.refreshAccessToken().pipe(
        switchMap((token) =>
          next(
            addAuthorizationHeader(
              request.clone({ context: request.context.set(RETRIED_WITH_REFRESH, true) }),
              token,
            ),
          ),
        ),
        catchError((refreshError) => {
          auth.logout(true, false);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

function addAuthorizationHeader(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token || !request.url.startsWith(environment.apiUrl) || isSessionRequest(request.url)) {
    return request;
  }

  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function shouldRefreshRequest(
  error: unknown,
  request: HttpRequest<unknown>,
  auth: AuthService,
): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === 401 &&
    request.url.startsWith(environment.apiUrl) &&
    !isSessionRequest(request.url) &&
    !request.context.get(RETRIED_WITH_REFRESH) &&
    Boolean(auth.session()?.refreshToken)
  );
}

function isSessionRequest(url: string): boolean {
  return url.endsWith('/auth/login') || url.endsWith('/auth/refresh') || url.endsWith('/auth/logout');
}