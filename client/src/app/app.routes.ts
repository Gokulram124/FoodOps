import { Routes } from '@angular/router';
import { roleGuard } from './core/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', loadComponent: () => import('./auth/login').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./auth/register').then(m => m.RegisterComponent) },

  {
    path: 'restaurants',
    canActivate: [roleGuard('Customer')],
    loadComponent: () => import('./customer/restaurant-list').then(m => m.RestaurantListComponent),
  },
  {
    path: 'restaurants/:id',
    canActivate: [roleGuard('Customer')],
    loadComponent: () => import('./customer/menu').then(m => m.MenuComponent),
  },
  {
    path: 'cart',
    canActivate: [roleGuard('Customer')],
    loadComponent: () => import('./customer/cart').then(m => m.CartComponent),
  },
  {
    path: 'orders',
    canActivate: [roleGuard('Customer')],
    loadComponent: () => import('./customer/my-orders').then(m => m.MyOrdersComponent),
  },
    {
    path: 'owner',
    canActivate: [roleGuard('RestaurantOwner')],
    loadComponent: () => import('./owner/owner-orders').then(m => m.OwnerOrdersComponent),
  },
  {
    path: 'owner/menu',
    canActivate: [roleGuard('RestaurantOwner')],
    loadComponent: () => import('./owner/owner-menu').then(m => m.OwnerMenuComponent),
  },
  {
    path: 'rider',
    canActivate: [roleGuard('DeliveryPartner')],
    loadComponent: () => import('./rider/rider-panel').then(m => m.RiderPanelComponent),
  },
    {
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    loadComponent: () => import('./admin/admin-dashboard').then(m => m.AdminDashboardComponent),
  },

  { path: '**', redirectTo: 'login' },
];