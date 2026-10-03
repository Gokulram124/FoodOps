import { badgeClass } from '../core/status';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { EMPTY, Subject, catchError, merge, switchMap, timer } from 'rxjs';
import { Order, OrderStatusLog } from '../core/models';
import { OrderService } from '../core/order.service';
import { toDate } from '../core/utc';

@Component({
  selector: 'app-my-orders',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 768px">
      <div class="d-flex align-items-center justify-content-between mb-4">
        <h1 class="h3 fw-semibold m-0">My orders</h1>
        <span class="small text-body-secondary">Updates every 10 seconds</span>
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="text-body-secondary">Loading orders...</p>
      } @else if (orders().length === 0) {
        <p class="text-body-secondary">You have not placed any orders yet.</p>
      } @else {
        @for (o of orders(); track o.id) {
          <div class="card mb-3">
            <div class="card-body">
              <div class="d-flex align-items-start justify-content-between gap-2">
                <div>
                  <div class="fw-medium">{{ o.restaurantName }}</div>
                  <div class="small text-body-secondary">Order #{{ o.id }} on {{ time(o.orderTime) | date: 'medium' }}</div>
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
                <div class="d-flex gap-2">
                  <button (click)="toggle(o.id)" class="btn btn-sm btn-outline-secondary">
                    {{ openId() === o.id ? 'Hide timeline' : 'Timeline' }}
                  </button>
                  @if (o.status === 'Placed') {
                    <button (click)="cancel(o.id)" class="btn btn-sm btn-outline-danger">Cancel</button>
                  }
                </div>
              </div>

              @if (openId() === o.id) {
                <ol class="list-unstyled border-start ps-3 small mt-3 mb-0">
                  @for (t of timeline(); track $index) {
                    <li class="mb-2">
                      <span class="fw-medium">{{ t.newStatus }}</span>
                      @if (t.remarks) { <span class="text-body-secondary"> - {{ t.remarks }}</span> }
                      <div class="text-body-secondary" style="font-size: 0.75rem">{{ time(t.changedOn) | date: 'medium' }}</div>
                    </li>
                  }
                </ol>
              }
            </div>
          </div>
        }
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

  readonly badge = badgeClass;

  private loadTimeline(id: number) {
    this.svc.timeline(id).subscribe(t => this.timeline.set(t));
  }
}