import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './config';
import { Delivery, Order, OrderStatus, Rider } from './models';

@Injectable({ providedIn: 'root' })
export class DeliveryService {
  private http = inject(HttpClient);

  setAvailability(isOnline: boolean, phone: string) {
    return this.http.put<Rider>(`${API_URL}/delivery/availability`, { isOnline, phone });
  }

  available() {
    return this.http.get<Order[]>(`${API_URL}/delivery/available`);
  }

  assign(orderId: number) {
    return this.http.post<Order>(`${API_URL}/delivery/assign/${orderId}`, {});
  }

  mine() {
    return this.http.get<Delivery[]>(`${API_URL}/delivery/my`);
  }

  updateStatus(orderId: number, newStatus: OrderStatus) {
    return this.http.put<Order>(`${API_URL}/delivery/status`, { orderId, newStatus });
  }
}