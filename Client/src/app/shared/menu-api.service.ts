import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MenuDto {
  id: number;
  restaurantId: number;
  name: string;
}

export interface CreateMenuRequest {
  restaurantId: number;
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class MenuApiService {
  private readonly http = inject(HttpClient);
  private readonly menusUrl = `${environment.apiUrl}/Menus`;

  getByRestaurantId(restaurantId: number): Observable<MenuDto[]> {
    return this.http.get<MenuDto[]>(`${this.menusUrl}/restaurant/${restaurantId}`);
  }

  getById(menuId: number): Observable<MenuDto> {
    return this.http.get<MenuDto>(`${this.menusUrl}/${menuId}`);
  }

  create(payload: CreateMenuRequest): Observable<MenuDto> {
    return this.http.post<MenuDto>(this.menusUrl, payload);
  }
}
