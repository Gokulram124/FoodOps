import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, finalize, firstValueFrom, shareReplay, tap, throwError } from 'rxjs';
import { API_URL } from './config';
import { AuthResponse, Role } from './models';

const KEY = 'foodops_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private state = signal<AuthResponse | null>(this.load());
  private refreshing$: Observable<AuthResponse> | null = null;

  user = this.state.asReadonly();
  token = computed(() => this.state()?.token ?? null);
  role = computed(() => this.state()?.role ?? null);
  // A session exists as long as we hold a refresh token. An expired access token is renewed silently.
  isLoggedIn = computed(() => !!this.state());

  login(body: { email: string; password: string }) {
    return this.http.post<AuthResponse>(`${API_URL}/auth/login`, body).pipe(tap(r => this.save(r)));
  }

  register(body: { fullName: string; email: string; password: string; role: Role }) {
    return this.http.post<AuthResponse>(`${API_URL}/auth/register`, body).pipe(tap(r => this.save(r)));
  }

  /** Single-flight: many parallel 401s share ONE refresh call (a second call would trip reuse detection). */
  refresh(): Observable<AuthResponse> {
    const rt = this.state()?.refreshToken;
    if (!rt) return throwError(() => new Error('No session'));

    if (!this.refreshing$) {
      this.refreshing$ = this.http
        .post<AuthResponse>(`${API_URL}/auth/refresh`, { refreshToken: rt })
        .pipe(
          tap(r => this.save(r)),
          finalize(() => (this.refreshing$ = null)),
          shareReplay(1)
        );
    }
    return this.refreshing$;
  }

  /** For SignalR: returns a valid token, refreshing first if it is about to expire. */
  async freshToken(): Promise<string> {
    const u = this.state();
    if (!u) return '';
    if (new Date(u.expiresAt).getTime() - Date.now() > 30_000) return u.token;
    try {
      return (await firstValueFrom(this.refresh())).token;
    } catch {
      return '';
    }
  }

  logout() {
    const rt = this.state()?.refreshToken;
    if (rt) {
      // tell the server to revoke it; ignore failures, we are leaving anyway
      this.http.post(`${API_URL}/auth/logout`, { refreshToken: rt }).subscribe({ error: () => {} });
    }
    this.expire();
  }

  /** Clear the local session (used on logout and when refresh fails). */
  expire() {
    localStorage.removeItem(KEY);
    this.state.set(null);
    this.router.navigate(['/login']);
  }

  homeFor(role: Role): string {
    switch (role) {
      case 'Customer': return '/restaurants';
      case 'RestaurantOwner': return '/owner';
      case 'DeliveryPartner': return '/rider';
      case 'Admin': return '/admin';
    }
  }

  private save(r: AuthResponse) {
    localStorage.setItem(KEY, JSON.stringify(r));
    this.state.set(r);
  }

  private load(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(KEY);
      if (!raw) return null;
      const s = JSON.parse(raw) as AuthResponse;
      return s.refreshToken ? s : null;   // sessions saved before refresh tokens existed: ask for a fresh login
    } catch {
      return null;
    }
  }
}