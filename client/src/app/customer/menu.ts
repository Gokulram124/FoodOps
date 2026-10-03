import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { CartService } from '../core/cart.service';
import { MenuService } from '../core/menu.service';
import { MenuItem, Restaurant } from '../core/models';
import { RestaurantService } from '../core/restaurant.service';

@Component({
  selector: 'app-menu',
  imports: [RouterLink, CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
      <a routerLink="/restaurants" class="small">Back to restaurants</a>

      @if (loading()) {
        <p class="mt-4 text-body-secondary">Loading menu...</p>
      } @else if (error()) {
        <div class="alert alert-danger py-2 small mt-4">{{ error() }}</div>
      } @else if (restaurant(); as r) {
        <div class="mt-3 mb-4">
          <h1 class="h3 fw-semibold mb-1">{{ r.name }}</h1>
          <p class="small text-body-secondary mb-0">{{ r.city }}</p>
          @if (!r.isOpen) {
            <div class="alert alert-secondary py-2 small mt-2 mb-0">
              This restaurant is closed right now, so you can't add items.
            </div>
          }
        </div>

        @if (notice()) {
          <div class="alert alert-warning d-flex align-items-center justify-content-between gap-2 py-2 small">
            <span>{{ notice() }}</span>
            <button (click)="clearCart()" class="btn btn-sm btn-outline-warning flex-shrink-0">Clear cart</button>
          </div>
        }

        <div class="row g-4">
          <div class="col-lg-8">
            @for (g of groups(); track g.category) {
              <section class="mb-4">
                <h2 class="h5 fw-medium mb-2">{{ g.category }}</h2>
                <div class="card">
                  <div class="list-group list-group-flush">
                    @for (m of g.items; track m.id) {
                      <div class="list-group-item d-flex align-items-center justify-content-between gap-3">
                        <div>
                          <div>{{ m.name }}</div>
                          <div class="small text-body-secondary">{{ m.price | currency: 'INR' : 'symbol' : '1.0-0' }}</div>
                        </div>
                        <button (click)="add(r, m)" [disabled]="!r.isOpen || !m.isAvailable" class="btn btn-sm btn-primary flex-shrink-0">
                          {{ m.isAvailable ? 'Add' : 'Unavailable' }}
                        </button>
                      </div>
                    }
                  </div>
                </div>
              </section>
            } @empty {
              <p class="text-body-secondary">This restaurant has not added any menu items yet.</p>
            }
          </div>

          <div class="col-lg-4">
            <aside class="card card-body">
              <h2 class="h6 fw-medium mb-3">Your cart</h2>
              @if (cart.lines().length === 0) {
                <p class="small text-body-secondary mb-0">Your cart is empty. Add an item to start.</p>
              } @else {
                <p class="small text-body-secondary mb-3">From {{ cart.restaurant()?.name }}</p>
                @for (l of cart.lines(); track l.item.id) {
                  <div class="d-flex align-items-center justify-content-between gap-2 small mb-2">
                    <span class="flex-fill">{{ l.item.name }}</span>
                    <div class="d-flex align-items-center gap-2">
                      <button (click)="cart.changeQuantity(l.item.id, -1)" class="btn btn-sm btn-outline-secondary py-0 px-2">-</button>
                      <span class="text-center" style="min-width: 1rem">{{ l.quantity }}</span>
                      <button (click)="cart.changeQuantity(l.item.id, 1)" class="btn btn-sm btn-outline-secondary py-0 px-2">+</button>
                    </div>
                    <span class="text-end" style="min-width: 4rem">{{ l.item.price * l.quantity | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
                  </div>
                }
                <div class="d-flex justify-content-between border-top pt-3 mt-3 fw-medium">
                  <span>Total</span>
                  <span>{{ cart.total() | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
                </div>
              }
            </aside>
          </div>
        </div>
      }
    </div>
  `,
})
export class MenuComponent {
  private route = inject(ActivatedRoute);
  private restaurants = inject(RestaurantService);
  private menuService = inject(MenuService);
  cart = inject(CartService);

  restaurant = signal<Restaurant | null>(null);
  items = signal<MenuItem[]>([]);
  loading = signal(true);
  error = signal('');
  notice = signal('');

  groups = computed(() => {
    const map = new Map<string, MenuItem[]>();
    for (const m of this.items()) {
      map.set(m.category, [...(map.get(m.category) ?? []), m]);
    }
    return [...map].map(([category, items]) => ({ category, items }));
  });

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    forkJoin({
      restaurant: this.restaurants.getById(id),
      menu: this.menuService.getByRestaurant(id),
    }).subscribe({
      next: ({ restaurant, menu }) => {
        this.restaurant.set(restaurant);
        this.items.set(menu);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.status === 404 ? 'This restaurant was not found.' : 'Could not load the menu. Check that the API is running.');
        this.loading.set(false);
      },
    });
  }

  add(restaurant: Restaurant, item: MenuItem) {
    const ok = this.cart.add(restaurant, item);
    this.notice.set(ok ? '' : `Your cart has items from ${this.cart.restaurant()?.name}. Clear it to order from ${restaurant.name}.`);
  }

  clearCart() {
    this.cart.clear();
    this.notice.set('');
  }
}