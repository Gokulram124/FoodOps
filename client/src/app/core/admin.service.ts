import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_URL } from './config';
import {
  AdminRestaurant, AdminStats, Coupon, CreateCouponRequest, CreateOwnerRequest,
  CreateRestaurantRequest, CustomerOption, OwnerSummary, Restaurant, RevenuePoint,
} from './models';

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

  revenue(period: string, restaurantId: number | null, ownerId: string | null) {
    return this.http.get<RevenuePoint[]>(`${API_URL}/admin/reports/revenue`, {
      params: this.reportParams(period, restaurantId, ownerId),
    });
  }

  revenueExcel(period: string, restaurantId: number | null, ownerId: string | null) {
    return this.http.get(`${API_URL}/admin/reports/revenue/export`, {
      params: this.reportParams(period, restaurantId, ownerId),
      responseType: 'blob',
    });
  }

  coupons() {
    return this.http.get<Coupon[]>(`${API_URL}/admin/coupons`);
  }

  createCoupon(body: CreateCouponRequest) {
    return this.http.post<Coupon>(`${API_URL}/admin/coupons`, body);
  }

  setCouponActive(id: number, isActive: boolean) {
    return this.http.put<Coupon>(`${API_URL}/admin/coupons/${id}/active`, { isActive });
  }

  customers() {
    return this.http.get<CustomerOption[]>(`${API_URL}/admin/customers`);
  }

  private reportParams(period: string, restaurantId: number | null, ownerId: string | null) {
    let params = new HttpParams()
      .set('period', period)
      .set('tzOffsetMinutes', -new Date().getTimezoneOffset());
    if (restaurantId !== null) params = params.set('restaurantId', restaurantId);
    if (ownerId !== null) params = params.set('ownerId', ownerId);
    return params;
  }
}