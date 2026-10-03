import { OrderStatus } from './models';

/** Tailwind classes for an order status badge. */
export function badgeClass(status: OrderStatus): string {
  switch (status) {
    case 'Delivered': return 'bg-emerald-500/20 text-emerald-300';
    case 'Rejected':
    case 'Cancelled': return 'bg-red-500/20 text-red-300';
    case 'Placed': return 'bg-amber-500/20 text-amber-300';
    default: return 'bg-indigo-500/20 text-indigo-300';
  }
}