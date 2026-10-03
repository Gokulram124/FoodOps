import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_URL } from './config';
import { MenuItem } from './models';

@Injectable({ providedIn: 'root' })
export class MenuService {
  private http = inject(HttpClient);

  getByRestaurant(restaurantId: number) {
    const params = new HttpParams().set('restaurantId', restaurantId);
    return this.http.get<MenuItem[]>(`${API_URL}/menu`, { params });
  }
}