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
    data: { title: 'Restaurant Panel' },
    loadComponent: () => import('./shared/placeholder').then(m => m.PlaceholderComponent),
  },
  {
    path: 'rider',
    canActivate: [roleGuard('DeliveryPartner')],
    data: { title: 'Delivery Panel' },
    loadComponent: () => import('./shared/placeholder').then(m => m.PlaceholderComponent),
  },
  {
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    data: { title: 'Admin Dashboard' },
    loadComponent: () => import('./shared/placeholder').then(m => m.PlaceholderComponent),
  },

  { path: '**', redirectTo: 'login' },
];