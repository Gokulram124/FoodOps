import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { EMPTY, Subject, catchError, merge, switchMap, timer } from 'rxjs';
import { Order, OrderStatus, OrderStatusLog } from '../core/models';
import { OrderService } from '../core/order.service';
import { toDate } from '../core/utc';

@Component({
  selector: 'app-my-orders',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <div class="mx-auto max-w-3xl px-6 py-8">
      <div class="mb-6 flex items-center justify-between">
        <h1 class="text-2xl font-semibold">My orders</h1>
        <span class="text-xs text-slate-500">Updates every 10 seconds</span>
      </div>

      @if (error()) {
        <div class="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="text-slate-400">Loading orders...</p>
      } @else if (orders().length === 0) {
        <p class="text-slate-400">You have not placed any orders yet.</p>
      } @else {
        <div class="space-y-4">
          @for (o of orders(); track o.id) {
            <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="font-medium">{{ o.restaurantName }}</p>
                  <p class="text-xs text-slate-400">Order #{{ o.id }} on {{ time(o.orderTime) | date: 'medium' }}</p>
                </div>
                <span class="rounded-full px-2 py-0.5 text-xs" [class]="badge(o.status)">{{ o.status }}</span>
              </div>

              <ul class="mt-3 space-y-1 text-sm text-slate-300">
                @for (i of o.items; track i.menuItemId) {
                  <li>{{ i.quantity }} x {{ i.name }}</li>
                }
              </ul>

              <div class="mt-3 flex items-center justify-between border-t border-white/10 pt-3 text-sm">
                <span class="font-medium">{{ o.totalAmount | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
                <div class="flex gap-2">
                  <button (click)="toggle(o.id)"
                          class="rounded-lg border border-white/10 px-3 py-1 hover:bg-white/10">
                    {{ openId() === o.id ? 'Hide timeline' : 'Timeline' }}
                  </button>
                  @if (o.status === 'Placed') {
                    <button (click)="cancel(o.id)"
                            class="rounded-lg border border-red-400/30 px-3 py-1 text-red-300 hover:bg-red-500/10">
                      Cancel
                    </button>
                  }
                </div>
              </div>

              @if (openId() === o.id) {
                <ol class="mt-4 space-y-2 border-l border-white/10 pl-4 text-sm">
                  @for (t of timeline(); track $index) {
                    <li>
                      <span class="font-medium">{{ t.newStatus }}</span>
                      @if (t.remarks) { <span class="text-slate-400"> - {{ t.remarks }}</span> }
                      <div class="text-xs text-slate-500">{{ time(t.changedOn) | date: 'medium' }}</div>
                    </li>
                  }
                </ol>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class MyOrdersComponent {
  private svc = inject(OrderService);
  private refresh$ = new Subject<void>();

  orders = signal<Order[]>([]);
  timeline = signal<OrderStatusLog[]>([]);
  openId = signal<number | null>(null);
  loading = signal(true);
  error = signal('');

  readonly time = toDate;

  constructor() {
    // Load now, then every 10s, and also whenever refresh$ fires. switchMap drops a slow old request.
    merge(timer(0, 10000), this.refresh$)
      .pipe(
        switchMap(() =>
          this.svc.my().pipe(
            catchError(() => {
              this.error.set('Could not load your orders. Check that the API is running.');
              this.loading.set(false);
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(list => {
        this.orders.set(list);
        this.error.set('');
        this.loading.set(false);
        const open = this.openId();
        if (open !== null) this.loadTimeline(open);
      });
  }

  toggle(id: number) {
    if (this.openId() === id) {
      this.openId.set(null);
      return;
    }
    this.openId.set(id);
    this.loadTimeline(id);
  }

  cancel(id: number) {
    this.svc.cancel(id).subscribe({
      next: () => this.refresh$.next(),
      error: err => this.error.set(err.error?.message ?? 'Could not cancel this order.'),
    });
  }

  badge(status: OrderStatus): string {
    switch (status) {
      case 'Delivered': return 'bg-emerald-500/20 text-emerald-300';
      case 'Rejected':
      case 'Cancelled': return 'bg-red-500/20 text-red-300';
      case 'Placed': return 'bg-amber-500/20 text-amber-300';
      default: return 'bg-indigo-500/20 text-indigo-300';
    }
  }

  private loadTimeline(id: number) {
    this.svc.timeline(id).subscribe(t => this.timeline.set(t));
  }
}