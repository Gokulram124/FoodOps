import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, merge } from 'rxjs';
import { API_URL } from './config';
import { AuthService } from './auth.service';

export interface OrderChanged {
  orderId: number;
  restaurantId: number;
  status: string;
  at: string;
}

const HUB_URL = API_URL.replace(/\/api$/, '') + '/hubs/orders';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private auth = inject(AuthService);
  private conn: signalR.HubConnection | null = null;

  readonly connected = signal(false);
  private orderChanged$ = new Subject<OrderChanged>();
  private reconnected$ = new Subject<void>();

  /** Pages subscribe to this: fires on a pushed event AND after a reconnect (events may have been missed). */
  readonly changes$ = merge(this.orderChanged$, this.reconnected$);

  start() {
    if (this.conn) return;

    this.conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => this.auth.token() ?? '' })
      .withAutomaticReconnect()
      .build();

    this.conn.on('orderChanged', (e: OrderChanged) => this.orderChanged$.next(e));
    this.conn.onreconnecting(() => this.connected.set(false));
    this.conn.onreconnected(() => {
      this.connected.set(true);
      this.reconnected$.next();   // refetch: we may have missed events while offline
    });
    this.conn.onclose(() => this.connected.set(false));

    this.conn.start()
      .then(() => this.connected.set(true))
      .catch(() => this.connected.set(false));
  }

  stop() {
    const c = this.conn;
    this.conn = null;
    this.connected.set(false);
    c?.stop();
  }
}