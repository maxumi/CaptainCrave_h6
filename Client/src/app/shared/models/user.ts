export enum Role {
  Customer = 'Customer',
  Restaurant = 'Restaurant',
  Admin = 'Admin',
}
export interface AuthState {
  userId: number;
  name: string;
  email: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
  role: string;
  token: string;
}

export type User = AuthState;