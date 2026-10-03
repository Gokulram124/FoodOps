import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { Subject, catchError, forkJoin, merge, of, switchMap, timer } from 'rxjs';
import { DeliveryService } from '../core/delivery.service';
import { Delivery, Order, OrderStatus } from '../core/models';
import { badgeClass } from '../core/status';

@Component({
  selector: 'app-rider-panel',
  imports: [ReactiveFormsModule, CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 896px">
      <h1 class="h3 fw-semibold mb-4">Delivery panel</h1>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      <div class="card card-body flex-row flex-wrap align-items-center gap-3 mb-4 small">
        <input [formControl]="phone" placeholder="Phone number" class="form-control w-auto" />
        <button (click)="toggleOnline()" class="btn" [class]="online() ? 'btn-outline-secondary' : 'btn-primary'">
          {{ online() ? 'Go offline' : 'Go online' }}
        </button>
        <span class="badge rounded-pill"
              [class]="online() ? 'bg-success-subtle text-success-emphasis' : 'bg-secondary-subtle text-secondary-emphasis'">
          {{ online() ? 'Online' : 'Offline' }}
        </span>
      </div>

      <section class="mb-4">
        <h2 class="h5 fw-medium mb-3">My deliveries</h2>
        @if (deliveries().length === 0) {
          <p class="small text-body-secondary">No deliveries yet. Accept an order below.</p>
        } @else {
          <div class="row g-3">
            @for (d of deliveries(); track d.id) {
              <div class="col-md-6">
                <div class="card h-100">
                  <div class="card-body">
                    <div class="d-flex align-items-center justify-content-between">
                      <span class="fw-medium">Order #{{ d.orderId }}</span>
                      <span class="badge rounded-pill" [class]="badge(d.orderStatus)">{{ d.orderStatus }}</span>
                    </div>

                    <div class="small mt-3">
                      @if (d.orderStatus === 'ReadyForPickup') {
                        <button (click)="move(d.orderId, 'PickedUp')" class="btn btn-sm btn-primary">Picked up</button>
                      } @else if (d.orderStatus === 'PickedUp') {
                        <button (click)="move(d.orderId, 'Delivered')" class="btn btn-sm btn-success">Mark delivered</button>
                      } @else if (d.orderStatus === 'Delivered') {
                        <span class="text-body-secondary">Completed</span>
                      } @else {
                        <span class="text-body-secondary">Waiting for the restaurant to finish preparing</span>
                      }
                    </div>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </section>

      <section>
        <h2 class="h5 fw-medium mb-3">Available orders</h2>
        @if (available().length === 0) {
          <p class="small text-body-secondary">No orders waiting for a rider right now.</p>
        } @else {
          <div class="row g-3">
            @for (o of available(); track o.id) {
              <div class="col-md-6">
                <div class="card h-100">
                  <div class="card-body">
                    <div class="d-flex align-items-start justify-content-between gap-2">
                      <div>
                        <div class="fw-medium">{{ o.restaurantName }}</div>
                        <div class="small text-body-secondary">Order #{{ o.id }}</div>
                      </div>
                      <span class="badge rounded-pill" [class]="badge(o.status)">{{ o.status }}</span>
                    </div>
                    <p class="small mt-3 mb-0">
                      {{ o.items.length }} item(s), {{ o.totalAmount | currency: 'INR' : 'symbol' : '1.0-0' }}
                    </p>
                    <button (click)="accept(o.id)" class="btn btn-sm btn-primary mt-3">Accept delivery</button>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </section>
    </div>
  `,
})
export class RiderPanelComponent {
  private svc = inject(DeliveryService);
  private refresh$ = new Subject<void>();

  phone = new FormControl('', { nonNullable: true });
  online = signal(false);
  available = signal<Order[]>([]);
  deliveries = signal<Delivery[]>([]);
  error = signal('');

  readonly badge = badgeClass;

  constructor() {
    merge(timer(0, 10000), this.refresh$)
      .pipe(
        switchMap(() =>
          forkJoin({
            available: this.svc.available().pipe(
              catchError(() => {
                this.error.set('Could not load orders. Check that the API is running.');
                return of([] as Order[]);
              })
            ),
            // A brand new rider has no profile yet, so this call can fail. That is expected, not an error.
            mine: this.svc.mine().pipe(catchError(() => of([] as Delivery[]))),
          })
        ),
        takeUntilDestroyed()
      )
      .subscribe(({ available, mine }) => {
        this.available.set(available);
        this.deliveries.set(mine);
      });
  }

  toggleOnline() {
    const goOnline = !this.online();
    if (goOnline && !this.phone.value.trim()) {
      this.error.set('Enter your phone number before going online.');
      return;
    }
    this.error.set('');

    this.svc.setAvailability(goOnline, this.phone.value.trim()).subscribe({
      next: r => {
        this.online.set(r.isOnline);
        this.refresh$.next();
      },
      error: err => this.error.set(err.error?.message ?? 'Could not change your status.'),
    });
  }

  accept(orderId: number) {
    this.error.set('');
    this.svc.assign(orderId).subscribe({
      next: () => this.refresh$.next(),
      error: err => {
        this.error.set(err.error?.message ?? 'Could not accept this order.');
        this.refresh$.next();
      },
    });
  }

  move(orderId: number, status: OrderStatus) {
    this.error.set('');
    this.svc.updateStatus(orderId, status).subscribe({
      next: () => this.refresh$.next(),
      error: err => this.error.set(err.error?.message ?? 'Could not update this delivery.'),
    });
  }
}