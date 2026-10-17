import { Injectable, computed, signal } from '@angular/core';
import { CartLine, CouponQuote, MenuItem, Restaurant } from './models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private _restaurant = signal<Restaurant | null>(null);
  private _lines = signal<CartLine[]>([]);
  private _coupon = signal<CouponQuote | null>(null);

  restaurant = this._restaurant.asReadonly();
  lines = this._lines.asReadonly();
  coupon = this._coupon.asReadonly();

  count = computed(() => this._lines().reduce((n, l) => n + l.quantity, 0));
  total = computed(() => this._lines().reduce((sum, l) => sum + l.item.price * l.quantity, 0));
  discount = computed(() => this._coupon()?.discount ?? 0);
  payable = computed(() => Math.max(0, this.total() - this.discount()));

  /** Returns false when the cart already holds items from a different restaurant. */
  add(restaurant: Restaurant, item: MenuItem): boolean {
    const current = this._restaurant();
    if (current && current.id !== restaurant.id && this._lines().length > 0) return false;

    this._restaurant.set(restaurant);
    this._coupon.set(null); // cart changed, so the old quote is stale
    this._lines.update(lines => {
      const found = lines.find(l => l.item.id === item.id);
      return found
        ? lines.map(l => (l.item.id === item.id ? { ...l, quantity: l.quantity + 1 } : l))
        : [...lines, { item, quantity: 1 }];
    });
    return true;
  }

  changeQuantity(itemId: number, delta: number) {
    this._coupon.set(null);
    this._lines.update(lines =>
      lines
        .map(l => (l.item.id === itemId ? { ...l, quantity: l.quantity + delta } : l))
        .filter(l => l.quantity > 0)
    );
    if (this._lines().length === 0) this._restaurant.set(null);
  }

  applyCoupon(quote: CouponQuote) {
    this._coupon.set(quote);
  }

  removeCoupon() {
    this._coupon.set(null);
  }

  clear() {
    this._lines.set([]);
    this._restaurant.set(null);
    this._coupon.set(null);
  }
}