import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from './config';
import { MenuItem } from './models';

export interface MenuItemForm {
  name: string;
  price: number;
  category: string;
}

export interface MyRestaurant {
  id: number;
  name: string;
  city: string;
  rating: number;
  isOpen: boolean;
}

@Injectable({ providedIn: 'root' })
export class OwnerMenuService {
  private http = inject(HttpClient);

  myRestaurants(): Observable<MyRestaurant[]> {
    return this.http.get<MyRestaurant[]>(`${API_URL}/restaurants/mine`);
  }

  list(restaurantId: number): Observable<MenuItem[]> {
    return this.http.get<MenuItem[]>(`${API_URL}/menu`, { params: { restaurantId } });
  }

  create(restaurantId: number, f: MenuItemForm): Observable<MenuItem> {
    return this.http.post<MenuItem>(`${API_URL}/menu`, { restaurantId, ...f });
  }

  update(id: number, f: MenuItemForm & { isAvailable: boolean }): Observable<MenuItem> {
    return this.http.put<MenuItem>(`${API_URL}/menu/${id}`, f);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API_URL}/menu/${id}`);
  }

  uploadImage(id: number, file: File): Observable<MenuItem> {
    const body = new FormData();
    body.append('file', file); // must match the IFormFile parameter name in MenuController
    return this.http.post<MenuItem>(`${API_URL}/menu/${id}/image`, body);
    // Don't set Content-Type manually. The browser adds multipart/form-data with the boundary.
  }
}

/** Backend returns "/uploads/menu/x.jpg" (relative). Prefix the API origin so <img> works on any host. */
export function imageSrc(url?: string | null): string | null {
  if (!url) return null;
  if (url.startsWith('http')) return url;
  return API_URL.replace(/\/api$/, '') + url;
}