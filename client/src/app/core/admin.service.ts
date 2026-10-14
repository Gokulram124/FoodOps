import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_URL } from './config';
import { AdminRestaurant, AdminStats, CreateOwnerRequest, CreateRestaurantRequest, OwnerSummary, Restaurant } from './models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);

  stats() {
    // Server stores UTC. Send our offset so "peak hour" is grouped in the admin's local time.
    const tz = -new Date().getTimezoneOffset();
    const params = new HttpParams().set('tzOffsetMinutes', tz);
    return this.http.get<AdminStats>(`${API_URL}/admin/stats`, { params });
  }

  owners() {
    return this.http.get<OwnerSummary[]>(`${API_URL}/admin/owners`);
  }

  createOwner(body: CreateOwnerRequest) {
    return this.http.post<OwnerSummary>(`${API_URL}/admin/owners`, body);
  }
    restaurants() {
    const tz = -new Date().getTimezoneOffset();
    const params = new HttpParams().set('tzOffsetMinutes', tz);
    return this.http.get<AdminRestaurant[]>(`${API_URL}/admin/restaurants`, { params });
  }

  createRestaurant(body: CreateRestaurantRequest) {
    return this.http.post<Restaurant>(`${API_URL}/admin/restaurants`, body);
  }
}