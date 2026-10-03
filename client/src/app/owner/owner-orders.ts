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
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 896px">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <h1 class="h3 fw-semibold m-0">Restaurant orders</h1>
        <div class="d-flex align-items-center gap-2">
          <button (click)="tab.set('active')" [class]="tabClass('active')">Active ({{ activeCount() }})</button>
          <button (click)="tab.set('all')" [class]="tabClass('all')">All ({{ orders().length }})</button>
        </div>
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="text-body-secondary">Loading orders...</p>
      } @else if (visible().length === 0) {
        <p class="text-body-secondary">No orders here yet. New orders appear automatically.</p>
      } @else {
        <div class="row g-3">
          @for (o of visible(); track o.id) {
            <div class="col-md-6">
              <div class="card h-100">
                <div class="card-body">
                  <div class="d-flex align-items-start justify-content-between gap-2">
                    <div>
                      <div class="fw-medium">Order #{{ o.id }}</div>
                      <div class="small text-body-secondary">{{ time(o.orderTime) | date: 'medium' }}</div>
                    </div>
                    <span class="badge rounded-pill" [class]="badge(o.status)">{{ o.status }}</span>
                  </div>

                  <ul class="list-unstyled small text-body-secondary mt-3 mb-0">
                    @for (i of o.items; track i.menuItemId) {
                      <li>{{ i.quantity }} x {{ i.name }}</li>
                    }
                  </ul>

                  <div class="d-flex align-items-center justify-content-between border-top pt-3 mt-3 small">
                    <span class="fw-medium">{{ o.totalAmount | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
                    <span class="text-body-secondary">{{ o.riderId ? 'Rider assigned' : 'No rider yet' }}</span>
                  </div>

                  @if (actionsFor(o.status).length > 0) {
                    <div class="d-flex flex-wrap gap-2 mt-3">
                      @for (a of actionsFor(o.status); track a.status) {
                        <button (click)="act(o, a)" [disabled]="busyId() === o.id" class="btn btn-sm"
                                [class]="a.danger ? 'btn-outline-danger' : 'btn-primary'">
                          {{ a.label }}
                        </button>
                      }
                    </div>
                  }
                </div>
              </div>
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
      ? 'btn btn-sm btn-primary'
      : 'btn btn-sm btn-outline-secondary';
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