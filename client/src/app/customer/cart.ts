import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../core/cart.service';
import { OrderService } from '../core/order.service';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 672px">
      <h1 class="h3 fw-semibold mb-4">Your cart</h1>

      @if (cart.lines().length === 0) {
        <div class="card card-body text-body-secondary">
          <span>Your cart is empty. <a routerLink="/restaurants">Browse restaurants</a></span>
        </div>
      } @else {
        <p class="small text-body-secondary mb-3">Ordering from {{ cart.restaurant()?.name }}</p>

        @if (error()) {
          <div class="alert alert-danger py-2 small">{{ error() }}</div>
        }

        <div class="card">
          <div class="list-group list-group-flush">
            @for (l of cart.lines(); track l.item.id) {
              <div class="list-group-item d-flex align-items-center justify-content-between gap-3 small">
                <span class="flex-fill">{{ l.item.name }}</span>
                <div class="d-flex align-items-center gap-2">
                  <button (click)="cart.changeQuantity(l.item.id, -1)" class="btn btn-sm btn-outline-secondary py-0 px-2">-</button>
                  <span class="text-center" style="min-width: 1rem">{{ l.quantity }}</span>
                  <button (click)="cart.changeQuantity(l.item.id, 1)" class="btn btn-sm btn-outline-secondary py-0 px-2">+</button>
                </div>
                <span class="text-end" style="min-width: 5rem">{{ l.item.price * l.quantity | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
              </div>
            }
          </div>
        </div>

        <div class="d-flex align-items-center justify-content-between fs-5 fw-medium mt-3">
          <span>Total</span>
          <span>{{ cart.total() | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
        </div>

        <button (click)="placeOrder()" [disabled]="placing()" class="btn btn-primary w-100 mt-4">
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