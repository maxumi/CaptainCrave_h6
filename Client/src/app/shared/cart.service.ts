import { Injectable, computed, signal } from '@angular/core';
import { MenuItemDto } from './menu-item-api.service';

export interface CartItem {
  menuItemId: number;
  restaurantId: number;
  name: string;
  price: number;
  imageUrl: string;
  quantity: number;
}

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly storageKey = 'cartItems';

  readonly items = signal<CartItem[]>(this.readStoredItems());
  readonly activeRestaurantId = computed(() => this.items()[0]?.restaurantId ?? null);

  readonly total = computed(() =>
    this.items().reduce((sum, item) => sum + item.price * item.quantity, 0)
  );

  addItem(menuItem: MenuItemDto, restaurantId: number): boolean {
    const activeRestaurantId = this.activeRestaurantId();

    if (activeRestaurantId !== null && activeRestaurantId !== restaurantId) {
      return false;
    }

    this.items.update((items) => {
      const existingIndex = items.findIndex((item) => item.menuItemId === menuItem.id);

      if (existingIndex >= 0) {
        const updated = [...items];
        updated[existingIndex] = {
          ...updated[existingIndex],
          quantity: updated[existingIndex].quantity + 1,
        };
        this.persist(updated);
        return updated;
      }

      const nextItems = [
        ...items,
        {
          menuItemId: menuItem.id,
          restaurantId,
          name: menuItem.name,
          price: menuItem.price,
          imageUrl: menuItem.imageUrl,
          quantity: 1,
        },
      ];

      this.persist(nextItems);
      return nextItems;
    });

    return true;
  }

  updateQuantity(menuItemId: number, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(menuItemId);
      return;
    }

    this.items.update((items) => {
      const updated = items.map((item) =>
        item.menuItemId === menuItemId ? { ...item, quantity } : item
      );

      this.persist(updated);
      return updated;
    });
  }

  removeItem(menuItemId: number): void {
    this.items.update((items) => {
      const updated = items.filter((item) => item.menuItemId !== menuItemId);
      this.persist(updated);
      return updated;
    });
  }

  clear(): void {
    this.persist([]);
    this.items.set([]);
  }

  private readStoredItems(): CartItem[] {
    const raw = localStorage.getItem(this.storageKey);

    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw) as CartItem[];

      if (!Array.isArray(parsed)) {
        return [];
      }

      return parsed.filter((item) =>
        Number.isFinite(item.menuItemId) &&
        Number.isFinite(item.restaurantId) &&
        typeof item.name === 'string' &&
        Number.isFinite(item.price) &&
        typeof item.imageUrl === 'string' &&
        Number.isFinite(item.quantity) &&
        item.quantity > 0
      );
    } catch {
      return [];
    }
  }

  private persist(items: CartItem[]): void {
    localStorage.setItem(this.storageKey, JSON.stringify(items));
  }
}
