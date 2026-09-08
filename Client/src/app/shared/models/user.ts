export enum Role {
  Customer = 'Customer',
  Restaurant = 'Restaurant',
  Admin = 'Admin',
}

// authentication state for a user
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

// Simple renaming for convenience
export type User = AuthState;