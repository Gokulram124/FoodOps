import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_URL } from './config';
import { PagedResult, Restaurant } from './models';

@Injectable({ providedIn: 'root' })
export class RestaurantService {
  private http = inject(HttpClient);

  getAll(search: string, page: number, pageSize: number) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);

    return this.http.get<PagedResult<Restaurant>>(`${API_URL}/restaurants`, { params });
  }
    getById(id: number) {
    return this.http.get<Restaurant>(`${API_URL}/restaurants/${id}`);
  }

  // Restaurants owned by the logged-in owner
  mine() {
    return this.http.get<Restaurant[]>(`${API_URL}/restaurants/mine`);
  }
}
