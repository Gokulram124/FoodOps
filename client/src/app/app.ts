import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { CartService } from './core/cart.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-slate-950 text-slate-100">
      <header class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="flex items-center gap-6">
          <span class="text-lg font-semibold tracking-tight">Food<span class="text-indigo-400">Ops</span></span>

          @if (auth.role() === 'Customer') {
            <nav class="flex items-center gap-4 text-sm text-slate-400">
              <a routerLink="/restaurants" routerLinkActive="text-white" class="hover:text-white">Restaurants</a>
              <a routerLink="/cart" routerLinkActive="text-white" class="hover:text-white">
                Cart
                @if (cart.count() > 0) {
                  <span class="ml-1 rounded-full bg-indigo-500 px-1.5 text-xs text-white">{{ cart.count() }}</span>
                }
              </a>
              <a routerLink="/orders" routerLinkActive="text-white" class="hover:text-white">My orders</a>
            </nav>
          }
        </div>

        @if (auth.isLoggedIn()) {
          <div class="flex items-center gap-4 text-sm">
            <span class="text-slate-300">{{ auth.user()?.fullName }}</span>
            <span class="rounded-full bg-indigo-500/20 px-2 py-0.5 text-xs text-indigo-300">{{ auth.role() }}</span>
            <button (click)="logout()" class="rounded-lg border border-white/10 px-3 py-1 hover:bg-white/10">
              Logout
            </button>
          </div>
        }
      </header>

      <main><router-outlet /></main>
    </div>
  `,
})
export class App {
  auth = inject(AuthService);
  cart = inject(CartService);

  logout() {
    this.cart.clear();
    this.auth.logout();
  }
}