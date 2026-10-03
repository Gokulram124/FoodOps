import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './config';
import { Order, OrderStatusLog } from './models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);

  place(restaurantId: number, items: { menuItemId: number; quantity: number }[]) {
    return this.http.post<Order>(`${API_URL}/orders`, { restaurantId, items });
  }

  my() {
    return this.http.get<Order[]>(`${API_URL}/orders/my`);
  }

  cancel(orderId: number) {
    return this.http.put<Order>(`${API_URL}/orders/status`, { orderId, newStatus: 'Cancelled' });
  }

  timeline(orderId: number) {
    return this.http.get<OrderStatusLog[]>(`${API_URL}/orders/${orderId}/timeline`);
  }
}