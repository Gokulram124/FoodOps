export type Role = 'Customer' | 'RestaurantOwner' | 'DeliveryPartner' | 'Admin';

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  role: Role;
  expiresAt: string;
  refreshToken: string;
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
  imageUrl?: string | null;
    stock?: number | null;   
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
  discountAmount?: number;
couponCode?: string | null;
}

export interface OrderStatusLog {
  oldStatus: OrderStatus;
  newStatus: OrderStatus;
  changedOn: string;
  remarks: string | null;
}

export interface Delivery {
  id: number;
  orderId: number;
  riderId: number;
  pickupTime: string | null;
  deliveredTime: string | null;
  orderStatus: OrderStatus;
}

export interface Rider {
  id: number;
  name: string;
  phone: string;
  isOnline: boolean;
}

export interface HourCount { hour: number; count: number; }

export interface DelayedOrder {
  id: number;
  restaurantName: string;
  status: OrderStatus;
  elapsedMinutes: number;
  risk: 'AtRisk' | 'Delayed';
}

export interface AdminStats {
  totalOrders: number;
  activeOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  revenue: number;
  avgDeliveryMinutes: number | null;
  delayedCount: number;
  atRiskCount: number;
  ordersByHour: HourCount[];
  delayedOrders: DelayedOrder[];
}
export interface OwnerSummary {
  id: string;
  fullName: string;
  email: string;
  restaurantCount: number;
}

export interface CreateOwnerRequest {
  fullName: string;
  email: string;
  password: string;
}
export interface AdminRestaurant {
  id: number;
  name: string;
  city: string;
  isOpen: boolean;
  ownerId: string;
  ownerName: string;
  ordersToday: number;
  revenueToday: number;
  activeNow: number;
}

export interface CreateRestaurantRequest {
  name: string;
  city: string;
  ownerId: string;
}
export interface RevenuePoint {
  label: string;
  orders: number;
  revenue: number;
}
export type DiscountType = 'Percent' | 'Flat';

export interface Coupon {
  id: number;
  code: string;
  type: DiscountType;
  value: number;
  maxDiscount: number | null;
  minOrderAmount: number;
  validFromUtc: string;
  validToUtc: string;
  assignedUserId: string | null;
  assignedUserName: string | null;
  usageLimit: number | null;
  usedCount: number;
  isActive: boolean;
  status: 'Active' | 'Scheduled' | 'Expired' | 'Used up' | 'Disabled';
}

export interface CreateCouponRequest {
  code: string;
  type: DiscountType;
  value: number;
  maxDiscount: number | null;
  minOrderAmount: number;
  validFromUtc: string;
  validToUtc: string;
  assignedUserId: string | null;
  usageLimit: number | null;
}

export interface CustomerOption {
  id: string;
  fullName: string;
  email: string;
}

export interface CouponQuote {
  code: string;
  subtotal: number;
  discount: number;
  total: number;
}