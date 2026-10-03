import { OrderStatus } from './models';

/** Bootstrap "subtle" badge colours for an order status (they adapt to light/dark theme). */
export function badgeClass(status: OrderStatus): string {
  switch (status) {
    case 'Delivered': return 'bg-success-subtle text-success-emphasis';
    case 'Rejected':
    case 'Cancelled': return 'bg-danger-subtle text-danger-emphasis';
    case 'Placed': return 'bg-warning-subtle text-warning-emphasis';
    default: return 'bg-primary-subtle text-primary-emphasis';
  }
}
