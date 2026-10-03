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
    <div class="mx-auto max-w-5xl px-6 py-8">
      <a routerLink="/restaurants" class="text-sm text-indigo-300 hover:underline">Back to restaurants</a>

      @if (loading()) {
        <p class="mt-6 text-slate-400">Loading menu...</p>
      } @else if (error()) {
        <div class="mt-6 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
      } @else if (restaurant(); as r) {
        <div class="mt-4 mb-6">
          <h1 class="text-2xl font-semibold">{{ r.name }}</h1>
          <p class="text-sm text-slate-400">{{ r.city }}</p>
          @if (!r.isOpen) {
            <p class="mt-2 rounded-lg bg-slate-500/20 px-3 py-2 text-sm text-slate-300">
              This restaurant is closed right now, so you can't add items.
            </p>
          }
        </div>

        @if (notice()) {
          <div class="mb-4 flex items-center justify-between rounded-lg bg-amber-500/10 px-3 py-2 text-sm text-amber-300">
            <span>{{ notice() }}</span>
            <button (click)="clearCart()" class="rounded border border-amber-300/30 px-2 py-0.5 hover:bg-amber-500/10">
              Clear cart
            </button>
          </div>
        }

        <div class="grid gap-8 lg:grid-cols-[1fr_320px]">
          <div class="space-y-6">
            @for (g of groups(); track g.category) {
              <section>
                <h2 class="mb-2 text-lg font-medium">{{ g.category }}</h2>
                <div class="divide-y divide-white/10 rounded-2xl border border-white/10 bg-white/5">
                  @for (m of g.items; track m.id) {
                    <div class="flex items-center justify-between gap-4 px-4 py-3">
                      <div>
                        <p>{{ m.name }}</p>
                        <p class="text-sm text-slate-400">{{ m.price | currency: 'INR' : 'symbol' : '1.0-0' }}</p>
                      </div>
                      <button (click)="add(r, m)" [disabled]="!r.isOpen || !m.isAvailable"
                              class="rounded-lg bg-indigo-500 px-3 py-1 text-sm hover:bg-indigo-400 disabled:opacity-40">
                        {{ m.isAvailable ? 'Add' : 'Unavailable' }}
                      </button>
                    </div>
                  }
                </div>
              </section>
            } @empty {
              <p class="text-slate-400">This restaurant has not added any menu items yet.</p>
            }
          </div>

          <aside class="h-fit rounded-2xl border border-white/10 bg-white/5 p-5">
            <h2 class="mb-3 font-medium">Your cart</h2>
            @if (cart.lines().length === 0) {
              <p class="text-sm text-slate-400">Your cart is empty. Add an item to start.</p>
            } @else {
              <p class="mb-3 text-sm text-slate-400">From {{ cart.restaurant()?.name }}</p>
              <div class="space-y-3">
                @for (l of cart.lines(); track l.item.id) {
                  <div class="flex items-center justify-between gap-2 text-sm">
                    <span class="flex-1">{{ l.item.name }}</span>
                    <div class="flex items-center gap-2">
                      <button (click)="cart.changeQuantity(l.item.id, -1)"
                              class="h-6 w-6 rounded border border-white/10 hover:bg-white/10">-</button>
                      <span class="w-4 text-center">{{ l.quantity }}</span>
                      <button (click)="cart.changeQuantity(l.item.id, 1)"
                              class="h-6 w-6 rounded border border-white/10 hover:bg-white/10">+</button>
                    </div>
                    <span class="w-16 text-right">{{ l.item.price * l.quantity | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
                  </div>
                }
              </div>
              <div class="mt-4 flex justify-between border-t border-white/10 pt-3 font-medium">
                <span>Total</span>
                <span>{{ cart.total() | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
              </div>
            }
          </aside>
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