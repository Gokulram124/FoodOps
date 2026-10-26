import { OrderStatus } from './models';

export interface DashboardSummary {
  totalOrders: number;
  cancelledOrders: number;
  revenue: number;
  avgOrderValue: number;
}
export interface DashboardTopRestaurant {
  restaurantId: number;
  name: string;
  orders: number;
  revenue: number;
}
export interface DashboardStatusCount {
  status: OrderStatus;
  orders: number;
}
export interface AnalyticsDashboard {
  summary: DashboardSummary;
  topRestaurants: DashboardTopRestaurant[];
  statusBreakdown: DashboardStatusCount[];
}
export interface LeaderboardRow {
  restaurantId: number;
  name: string;
  city: string;
  orders: number;
  revenue: number;
  overallRank: number;
  cityRank: number;
  sharePercent: number;
}

export interface RevenueTrendPoint {
  period: string;
  orderCount: number;
  revenue: number;
  discounts: number;
}