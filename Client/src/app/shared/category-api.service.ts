import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CategoryDto {
  id: number;
  menuId: number;
  name: string;
}

export interface CreateCategoryRequest {
  menuId: number;
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class CategoryApiService {
  private readonly http = inject(HttpClient);
  private readonly categoriesUrl = `${environment.apiUrl}/Categories`;

  getByRestaurantId(restaurantId: number): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.categoriesUrl}/restaurant/${restaurantId}`);
  }

  getByMenuId(menuId: number): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.categoriesUrl}/menu/${menuId}`);
  }

  create(payload: CreateCategoryRequest): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(this.categoriesUrl, payload);
  }
}
