export type Role = 'Customer' | 'RestaurantOwner' | 'DeliveryPartner' | 'Admin';

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  role: Role;
  expiresAt: string;
}

export interface Restaurant {
  id: number;
  name: string;
  city: string;
  rating: number;
  isOpen: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface MenuItem {
  id: number;
  restaurantId: number;
  name: string;
  price: number;
  category: string;
  isAvailable: boolean;
}

export interface CartLine {
  item: MenuItem;
  quantity: number;
}

export type OrderStatus =
  | 'Placed' | 'Accepted' | 'Preparing' | 'ReadyForPickup'
  | 'PickedUp' | 'Delivered' | 'Rejected' | 'Cancelled';

export interface OrderItem {
  menuItemId: number;
  name: string;
  quantity: number;
  price: number;
}

export interface Order {
  id: number;
  restaurantId: number;
  restaurantName: string;
  status: OrderStatus;
  totalAmount: number;
  orderTime: string;
  riderId: number | null;
  items: OrderItem[];
}

export interface OrderStatusLog {
  oldStatus: OrderStatus;
  newStatus: OrderStatus;
  changedOn: string;
  remarks: string | null;
}