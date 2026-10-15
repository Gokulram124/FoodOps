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

export interface StockChanged {
  restaurantId: number;
  items: { menuItemId: number; stock: number | null }[];
}

const HUB_URL = API_URL.replace(/\/api$/, '') + '/hubs/orders';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private auth = inject(AuthService);
  private conn: signalR.HubConnection | null = null;
  private menus = new Set<number>();   // menu groups this browser wants to be in

  readonly connected = signal(false);
  private orderChanged$ = new Subject<OrderChanged>();
  private reconnected$ = new Subject<void>();

  readonly stockChanged$ = new Subject<StockChanged>();
  readonly reconnects$ = this.reconnected$.asObservable();

  /** Pages subscribe to this: fires on a pushed event AND after a reconnect (events may have been missed). */
  readonly changes$ = merge(this.orderChanged$, this.reconnected$);

  start() {
    if (this.conn) return;

    this.conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => this.auth.freshToken() })
      .withAutomaticReconnect()
      .build();

    this.conn.on('orderChanged', (e: OrderChanged) => this.orderChanged$.next(e));
    this.conn.on('stockChanged', (e: StockChanged) => {
      console.log('[rt] stockChanged', e);
      this.stockChanged$.next(e);
    });
    this.conn.onreconnecting(() => this.connected.set(false));
    this.conn.onreconnected(() => {
      this.connected.set(true);
      this.rejoinMenus();          // a new connection id has no groups yet
      this.reconnected$.next();    // refetch: we may have missed events while offline
    });
    this.conn.onclose(() => this.connected.set(false));

    this.conn.start()
      .then(() => {
        this.connected.set(true);
        this.rejoinMenus();        // a page may have asked to join before we were connected
      })
      .catch(() => this.connected.set(false));
  }

  joinMenu(restaurantId: number) {
    this.menus.add(restaurantId);
    this.invoke('JoinMenu', restaurantId);
  }

  leaveMenu(restaurantId: number) {
    this.menus.delete(restaurantId);
    this.invoke('LeaveMenu', restaurantId);
  }

  stop() {
    const c = this.conn;
    this.conn = null;
    this.menus.clear();
    this.connected.set(false);
    c?.stop();
  }

  private rejoinMenus() {
    this.menus.forEach(id => this.invoke('JoinMenu', id));
  }

  private invoke(method: string, restaurantId: number) {
    const ok = this.conn?.state === signalR.HubConnectionState.Connected;
    console.log('[rt]', method, restaurantId, 'connected:', ok);
    if (ok) this.conn!.invoke(method, restaurantId).catch(e => console.warn('[rt] failed', method, e));
  }
}