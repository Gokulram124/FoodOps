export type Role = 'Customer' | 'RestaurantOwner' | 'DeliveryPartner' | 'Admin';

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  role: Role;
  expiresAt: string;
}
