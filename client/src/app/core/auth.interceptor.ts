import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_URL } from './config';
import { AuthService } from './auth.service';

const isAuthCall = (url: string) => /\/auth\/(login|register|refresh|logout)$/.test(url);

function withToken(req: HttpRequest<unknown>, token: string | null) {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // only talk "bearer" to our own API, and never loop on the auth endpoints
  if (!req.url.startsWith(API_URL) || isAuthCall(req.url)) return next(req);

  return next(withToken(req, auth.token())).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401 || !auth.user()) {
        return throwError(() => err);
      }
      return auth.refresh().pipe(
        // refresh itself failed => session is dead => back to login
        catchError(e => {
          auth.expire();
          return throwError(() => e);
        }),
        // refresh worked => replay the original request with the new token
        switchMap(r => next(withToken(req, r.token)))
      );
    })
  );
};