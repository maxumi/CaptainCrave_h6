import { Service } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';


export interface ReviewDto {
  id: number;
  userId: number;
  restaurantId: number;
  rating: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReviewDto {
  restaurantId: number;
  rating: number;
}

export interface UpdateReviewDto {
  rating: number;
}

export interface RestaurantReviewSummaryDto {
  restaurantId: number;
  averageRating: number;
  reviewCount: number;
}

@Service()
export class ReviewApiService {
  private readonly http = inject(HttpClient);
  private readonly reviewsUrl = `${environment.apiUrl}/Reviews`;

getByRestaurant(restaurantId: number): Observable<RestaurantReviewSummaryDto> {
  return this.http.get<RestaurantReviewSummaryDto>(
    `${this.reviewsUrl}/restaurant/${restaurantId}`
  );
}

getMyReviewByRestaurant(
  restaurantId: number
): Observable<ReviewDto | null> {
  return this.http.get<ReviewDto | null>(
    `${this.reviewsUrl}/restaurant/${restaurantId}/mine`
  );
}

create(dto: CreateReviewDto): Observable<ReviewDto> {
  return this.http.post<ReviewDto>(this.reviewsUrl, dto);
}

update(reviewId: number, dto: UpdateReviewDto): Observable<ReviewDto> {
  return this.http.put<ReviewDto>(
    `${this.reviewsUrl}/${reviewId}`,
    dto
  );
}

}
