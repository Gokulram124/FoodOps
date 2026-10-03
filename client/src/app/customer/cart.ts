import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../core/cart.service';
import { OrderService } from '../core/order.service';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, CurrencyPipe],
  template: `
    <div class="mx-auto max-w-2xl px-6 py-8">
      <h1 class="mb-6 text-2xl font-semibold">Your cart</h1>

      @if (cart.lines().length === 0) {
        <div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-slate-400">
          Your cart is empty.
          <a routerLink="/restaurants" class="text-indigo-300 hover:underline">Browse restaurants</a>
        </div>
      } @else {
        <p class="mb-4 text-sm text-slate-400">Ordering from {{ cart.restaurant()?.name }}</p>

        @if (error()) {
          <div class="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
        }

        <div class="divide-y divide-white/10 rounded-2xl border border-white/10 bg-white/5">
          @for (l of cart.lines(); track l.item.id) {
            <div class="flex items-center justify-between gap-3 px-4 py-3 text-sm">
              <span class="flex-1">{{ l.item.name }}</span>
              <div class="flex items-center gap-2">
                <button (click)="cart.changeQuantity(l.item.id, -1)"
                        class="h-6 w-6 rounded border border-white/10 hover:bg-white/10">-</button>
                <span class="w-4 text-center">{{ l.quantity }}</span>
                <button (click)="cart.changeQuantity(l.item.id, 1)"
                        class="h-6 w-6 rounded border border-white/10 hover:bg-white/10">+</button>
              </div>
              <span class="w-20 text-right">{{ l.item.price * l.quantity | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
            </div>
          }
        </div>

        <div class="mt-4 flex items-center justify-between text-lg font-medium">
          <span>Total</span>
          <span>{{ cart.total() | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
        </div>

        <button (click)="placeOrder()" [disabled]="placing()"
                class="mt-6 w-full rounded-lg bg-indigo-500 py-2 font-medium hover:bg-indigo-400 disabled:opacity-50">
          {{ placing() ? 'Placing order...' : 'Place order' }}
        </button>
      }
    </div>
  `,
})
export class CartComponent {
  cart = inject(CartService);
  private orders = inject(OrderService);
  private router = inject(Router);

  placing = signal(false);
  error = signal('');

  placeOrder() {
    const restaurant = this.cart.restaurant();
    if (!restaurant || this.cart.lines().length === 0) return;

    this.placing.set(true);
    this.error.set('');

    const items = this.cart.lines().map(l => ({ menuItemId: l.item.id, quantity: l.quantity }));

    this.orders.place(restaurant.id, items).subscribe({
      next: () => {
        this.cart.clear();
        this.router.navigate(['/orders']);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Could not place the order. Please try again.');
        this.placing.set(false);
      },
    });
  }
}