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
    <div class="mx-auto max-w-4xl px-6 py-8">
      <h1 class="mb-6 text-2xl font-semibold">Delivery panel</h1>

      @if (error()) {
        <div class="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
      }

      <div class="mb-8 flex flex-wrap items-center gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 text-sm">
        <input [formControl]="phone" placeholder="Phone number"
               class="rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
        <button (click)="toggleOnline()"
                class="rounded-lg px-3 py-2"
                [class]="online() ? 'border border-white/10 hover:bg-white/10' : 'bg-indigo-500 hover:bg-indigo-400'">
          {{ online() ? 'Go offline' : 'Go online' }}
        </button>
        <span class="rounded-full px-2 py-0.5 text-xs"
              [class]="online() ? 'bg-emerald-500/20 text-emerald-300' : 'bg-slate-500/20 text-slate-400'">
          {{ online() ? 'Online' : 'Offline' }}
        </span>
      </div>

      <section class="mb-8">
        <h2 class="mb-3 text-lg font-medium">My deliveries</h2>
        @if (deliveries().length === 0) {
          <p class="text-sm text-slate-400">No deliveries yet. Accept an order below.</p>
        } @else {
          <div class="grid gap-4 md:grid-cols-2">
            @for (d of deliveries(); track d.id) {
              <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
                <div class="flex items-center justify-between">
                  <p class="font-medium">Order #{{ d.orderId }}</p>
                  <span class="rounded-full px-2 py-0.5 text-xs" [class]="badge(d.orderStatus)">{{ d.orderStatus }}</span>
                </div>

                <div class="mt-3 text-sm">
                  @if (d.orderStatus === 'ReadyForPickup') {
                    <button (click)="move(d.orderId, 'PickedUp')"
                            class="rounded-lg bg-indigo-500 px-3 py-1 hover:bg-indigo-400">Picked up</button>
                  } @else if (d.orderStatus === 'PickedUp') {
                    <button (click)="move(d.orderId, 'Delivered')"
                            class="rounded-lg bg-emerald-500 px-3 py-1 hover:bg-emerald-400">Mark delivered</button>
                  } @else if (d.orderStatus === 'Delivered') {
                    <span class="text-slate-400">Completed</span>
                  } @else {
                    <span class="text-slate-400">Waiting for the restaurant to finish preparing</span>
                  }
                </div>
              </div>
            }
          </div>
        }
      </section>

      <section>
        <h2 class="mb-3 text-lg font-medium">Available orders</h2>
        @if (available().length === 0) {
          <p class="text-sm text-slate-400">No orders waiting for a rider right now.</p>
        } @else {
          <div class="grid gap-4 md:grid-cols-2">
            @for (o of available(); track o.id) {
              <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
                <div class="flex items-start justify-between gap-2">
                  <div>
                    <p class="font-medium">{{ o.restaurantName }}</p>
                    <p class="text-xs text-slate-400">Order #{{ o.id }}</p>
                  </div>
                  <span class="rounded-full px-2 py-0.5 text-xs" [class]="badge(o.status)">{{ o.status }}</span>
                </div>
                <p class="mt-3 text-sm text-slate-300">
                  {{ o.items.length }} item(s), {{ o.totalAmount | currency: 'INR' : 'symbol' : '1.0-0' }}
                </p>
                <button (click)="accept(o.id)"
                        class="mt-3 rounded-lg bg-indigo-500 px-3 py-1 text-sm hover:bg-indigo-400">
                  Accept delivery
                </button>
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