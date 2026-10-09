import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { CartService } from './core/cart.service';
import { ThemeService } from './core/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-vh-100">
      <nav class="navbar flex-wrap gap-2 border-bottom px-3 px-md-4 py-3">
        <div class="d-flex align-items-center gap-4">
          <span class="navbar-brand fw-semibold m-0">Food<span class="text-primary">Ops</span></span>

          @if (auth.role() === 'Customer') {
            <ul class="nav gap-1">
              <li class="nav-item"><a routerLink="/restaurants" routerLinkActive="active" class="nav-link py-1 px-2">Restaurants</a></li>
              <li class="nav-item">
                <a routerLink="/cart" routerLinkActive="active" class="nav-link py-1 px-2">
                  Cart
                  @if (cart.count() > 0) {
                    <span class="badge rounded-pill text-bg-primary ms-1">{{ cart.count() }}</span>
                  }
                </a>
              </li>
              <li class="nav-item"><a routerLink="/orders" routerLinkActive="active" class="nav-link py-1 px-2">My orders</a></li>
            </ul>
          }

          @if (auth.role() === 'RestaurantOwner') {
            <ul class="nav gap-1">
              <li class="nav-item"><a routerLink="/owner" [routerLinkActiveOptions]="{ exact: true }" routerLinkActive="active" class="nav-link py-1 px-2">Orders</a></li>
              <li class="nav-item"><a routerLink="/owner/menu" routerLinkActive="active" class="nav-link py-1 px-2">Menu</a></li>
            </ul>
          }
        </div>

        <div class="d-flex align-items-center gap-2 gap-md-3 small">
          <button (click)="theme.toggle()" [attr.aria-label]="'Switch to ' + (theme.theme() === 'dark' ? 'light' : 'dark') + ' theme'"
                  class="btn btn-sm btn-outline-secondary">
            {{ theme.theme() === 'dark' ? 'Light mode' : 'Dark mode' }}
          </button>

          @if (auth.isLoggedIn()) {
            <span class="text-body-secondary d-none d-sm-inline">{{ auth.user()?.fullName }}</span>
            <span class="badge rounded-pill bg-primary-subtle text-primary-emphasis">{{ auth.role() }}</span>
            <button (click)="logout()" class="btn btn-sm btn-outline-secondary">Logout</button>
          }
        </div>
      </nav>

      <main><router-outlet /></main>
    </div>
  `,
})
export class App {
  auth = inject(AuthService);
  cart = inject(CartService);
  theme = inject(ThemeService);

  logout() {
    this.cart.clear();
    this.auth.logout();
  }
}