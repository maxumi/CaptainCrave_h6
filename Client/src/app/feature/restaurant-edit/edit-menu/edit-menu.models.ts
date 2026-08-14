export interface RestaurantDto {
  id: number;
  userId: number;
  name: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  imageUrl: string;
  isActive: boolean;
  createdAt: string;
}

export interface MenuItem {
  id: number;
  restaurantId: number;
  categoryId: number | null;
  name: string;
  description: string;
  imageUrl: string;
  price: number;
  isAvailable: boolean;
}

export interface UpdateMenuItemRequest {
  restaurantId: number;
  categoryId: number | null;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
  isAvailable: boolean;
}

export type MenuEditMode = 'create' | 'edit';