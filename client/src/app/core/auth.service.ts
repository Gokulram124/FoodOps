import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { API_URL } from './config';
import { AuthResponse, Role } from './models';

const KEY = 'foodops_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private state = signal<AuthResponse | null>(this.load());

  user = this.state.asReadonly();
  token = computed(() => this.state()?.token ?? null);
  role = computed(() => this.state()?.role ?? null);
  isLoggedIn = computed(() => {
    const u = this.state();
    return !!u && new Date(u.expiresAt) > new Date();
  });

  login(body: { email: string; password: string }) {
    return this.http.post<AuthResponse>(`${API_URL}/auth/login`, body).pipe(tap(r => this.save(r)));
  }

  register(body: { fullName: string; email: string; password: string; role: Role }) {
    return this.http.post<AuthResponse>(`${API_URL}/auth/register`, body).pipe(tap(r => this.save(r)));
  }

  logout() {
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
      return raw ? (JSON.parse(raw) as AuthResponse) : null;
    } catch {
      return null;
    }
  }
}
