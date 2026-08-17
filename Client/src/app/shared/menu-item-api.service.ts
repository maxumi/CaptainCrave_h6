import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MenuItemDto {
  id: number;
  restaurantId: number;
  categoryId: number | null;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
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

@Injectable({
  providedIn: 'root',
})
export class MenuItemApiService {
  private readonly http = inject(HttpClient);
  private readonly menuItemsUrl = `${environment.apiUrl}/MenuItems`;

  getByRestaurantId(restaurantId: number): Observable<MenuItemDto[]> {
    return this.http.get<MenuItemDto[]>(`${this.menuItemsUrl}/restaurant/${restaurantId}`);
  }

  getMenuItemsByRestaurant(restaurantId: number): Observable<MenuItemDto[]> {
    return this.http.get<MenuItemDto[]>(`${this.menuItemsUrl}/restaurant/${restaurantId}`);
  }

  create(payload: UpdateMenuItemRequest): Observable<MenuItemDto> {
    return this.http.post<MenuItemDto>(this.menuItemsUrl, payload);
  }

  update(itemId: number, payload: UpdateMenuItemRequest): Observable<MenuItemDto> {
    return this.http.put<MenuItemDto>(`${this.menuItemsUrl}/${itemId}`, payload);
  }

  delete(itemId: number): Observable<void> {
    return this.http.delete<void>(`${this.menuItemsUrl}/${itemId}`);
  }
}