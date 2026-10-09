import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_URL } from './config';
import { MenuItem } from './models';

export interface MenuItemInput {
  restaurantId: number;
  name: string;
  price: number;
  category: string;
  isAvailable: boolean;
}

@Injectable({ providedIn: 'root' })
export class MenuService {
  private http = inject(HttpClient);

  getByRestaurant(restaurantId: number) {
    const params = new HttpParams().set('restaurantId', restaurantId);
    return this.http.get<MenuItem[]>(`${API_URL}/menu`, { params });
  }

  create(input: MenuItemInput) {
    return this.http.post<MenuItem>(`${API_URL}/menu`, input);
  }

  update(id: number, input: MenuItemInput) {
    return this.http.put<MenuItem>(`${API_URL}/menu/${id}`, input);
  }

  remove(id: number) {
    return this.http.delete<void>(`${API_URL}/menu/${id}`);
  }

  uploadImage(id: number, file: File) {
    const form = new FormData();
    form.append('file', file); // must match IFormFile parameter name on the API
    return this.http.post<MenuItem>(`${API_URL}/menu/${id}/image`, form);
  }
}
