import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateRestaurantRequest {
  userId: number;
  name: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  imageUrl?: string;
  isActive: boolean;
}

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

@Injectable({
  providedIn: 'root',
})
export class RestaurantApiService {
  private readonly http = inject(HttpClient);
  private readonly restaurantsUrl = `${environment.apiUrl}/Restaurants`;

  createRestaurant(payload: CreateRestaurantRequest): Observable<RestaurantDto> {
    return this.http.post<RestaurantDto>(this.restaurantsUrl, payload);
  }

  getRestaurants(): Observable<RestaurantDto[]> {
    return this.http.get<RestaurantDto[]>(this.restaurantsUrl);
  }

  getMyRestaurant(): Observable<RestaurantDto> {
    return this.http.get<RestaurantDto>(`${this.restaurantsUrl}/me`);
  }

  getById(restaurantId: number): Observable<RestaurantDto> {
    return this.http.get<RestaurantDto>(`${this.restaurantsUrl}/${restaurantId}`);
  }

  getNearbyRestaurants(
    latitude: number,
    longitude: number,
    radiusKm = 10
  ): Observable<RestaurantDto[]> {
    return this.http.get<RestaurantDto[]>(`${this.restaurantsUrl}/nearby`, {
      params: {
        latitude,
        longitude,
        radiusKm,
      },
    });
  }

  uploadImage(restaurantId: number, file: File): Observable<RestaurantDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<RestaurantDto>(`${this.restaurantsUrl}/${restaurantId}/image`, formData);
  }
}