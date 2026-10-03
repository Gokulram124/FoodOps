import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { EMPTY, Subject, catchError, merge, switchMap, timer } from 'rxjs';
import { Order, OrderStatus } from '../core/models';
import { OrderService } from '../core/order.service';
import { badgeClass } from '../core/status';
import { toDate } from '../core/utc';

interface Action {
  label: string;
  status: OrderStatus;
  danger?: boolean;
}

// What the restaurant can do next from each status (same rules as the .NET API)
const ACTIONS: Partial<Record<OrderStatus, Action[]>> = {
  Placed: [
    { label: 'Accept', status: 'Accepted' },
    { label: 'Reject', status: 'Rejected', danger: true },
  ],
  Accepted: [{ label: 'Start preparing', status: 'Preparing' }],
  Preparing: [{ label: 'Mark ready for pickup', status: 'ReadyForPickup' }],
};

const IN_PROGRESS: OrderStatus[] = ['Placed', 'Accepted', 'Preparing', 'ReadyForPickup', 'PickedUp'];

@Component({
  selector: 'app-owner-orders',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <div class="mx-auto max-w-4xl px-6 py-8">
      <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h1 class="text-2xl font-semibold">Restaurant orders</h1>
        <div class="flex items-center gap-3 text-sm">
          <button (click)="tab.set('active')" [class]="tabClass('active')">Active ({{ activeCount() }})</button>
          <button (click)="tab.set('all')" [class]="tabClass('all')">All ({{ orders().length }})</button>
        </div>
      </div>

      @if (error()) {
        <div class="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="text-slate-400">Loading orders...</p>
      } @else if (visible().length === 0) {
        <p class="text-slate-400">No orders here yet. New orders appear automatically.</p>
      } @else {
        <div class="grid gap-4 md:grid-cols-2">
          @for (o of visible(); track o.id) {
            <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="font-medium">Order #{{ o.id }}</p>
                  <p class="text-xs text-slate-400">{{ time(o.orderTime) | date: 'medium' }}</p>
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
                <span class="text-xs text-slate-400">{{ o.riderId ? 'Rider assigned' : 'No rider yet' }}</span>
              </div>

              @if (actionsFor(o.status).length > 0) {
                <div class="mt-3 flex flex-wrap gap-2">
                  @for (a of actionsFor(o.status); track a.status) {
                    <button (click)="act(o, a)" [disabled]="busyId() === o.id"
                            class="rounded-lg px-3 py-1 text-sm disabled:opacity-50"
                            [class]="a.danger
                              ? 'border border-red-400/30 text-red-300 hover:bg-red-500/10'
                              : 'bg-indigo-500 hover:bg-indigo-400'">
                      {{ a.label }}
                    </button>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class OwnerOrdersComponent {
  private svc = inject(OrderService);
  private refresh$ = new Subject<void>();

  orders = signal<Order[]>([]);
  tab = signal<'active' | 'all'>('active');
  loading = signal(true);
  error = signal('');
  busyId = signal<number | null>(null);

  readonly time = toDate;
  readonly badge = badgeClass;

  activeCount = computed(() => this.orders().filter(o => IN_PROGRESS.includes(o.status)).length);
  visible = computed(() =>
    this.tab() === 'all' ? this.orders() : this.orders().filter(o => IN_PROGRESS.includes(o.status))
  );

  constructor() {
    merge(timer(0, 10000), this.refresh$)
      .pipe(
        switchMap(() =>
          this.svc.restaurantOrders().pipe(
            catchError(() => {
              this.error.set('Could not load orders. Check that the API is running.');
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
      });
  }

  actionsFor(status: OrderStatus): Action[] {
    return ACTIONS[status] ?? [];
  }

  tabClass(t: 'active' | 'all'): string {
    return this.tab() === t
      ? 'rounded-lg bg-indigo-500 px-3 py-1'
      : 'rounded-lg border border-white/10 px-3 py-1 text-slate-300 hover:bg-white/10';
  }

  act(order: Order, action: Action) {
    this.busyId.set(order.id);
    this.error.set('');

    this.svc.updateStatus(order.id, action.status).subscribe({
      next: () => {
        this.busyId.set(null);
        this.refresh$.next();
      },
      error: err => {
        this.busyId.set(null);
        this.error.set(err.error?.message ?? 'Could not update this order.');
        this.refresh$.next();
      },
    });
  }
}