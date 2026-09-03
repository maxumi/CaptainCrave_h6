import { Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import {
  RestaurantReviewSummaryDto,
  ReviewApiService,
  ReviewDto,
} from '../../../shared/review-api.service';
import { OrderApiService } from '../../../shared/order-api.service';
import { Role } from '../../../shared/models/user';

@Component({
  selector: 'app-restaurant-reviews',
  imports: [],
  templateUrl: './restaurant-reviews.html',
  styleUrl: './restaurant-reviews.css',
})
export class RestaurantReviews implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly reviewApiService = inject(ReviewApiService);
  private readonly orderApiService = inject(OrderApiService);

  // Give id to use of Api call.
  readonly restaurantId = input.required<number>();

  readonly reviewSummary = signal<RestaurantReviewSummaryDto | null>(null);
  readonly currentUserReview = signal<ReviewDto | null>(null);

  readonly reviewRating = signal(0);
  readonly reviewMessage = signal<string | null>(null);
  readonly hasOrdered = signal(false);

  readonly canReview = computed(
    () =>
      this.authService.user()?.role === Role.Customer &&
      this.hasOrdered()
  );

  ngOnInit(): void {
    this.loadReviews();

    if (this.authService.user()?.role === Role.Customer) {
      this.loadHasOrdered();
      this.loadCurrentUserReview();
    }
  }

  setRating(rating: number): void {
    this.reviewRating.set(rating);
    this.reviewMessage.set(null);
  }

  saveReview(): void {
    if (!this.canReview()) {
      this.reviewMessage.set(
        'You can only review restaurants you have ordered from.'
      );
      return;
    }

    const rating = this.reviewRating();

    if (rating < 1 || rating > 5) {
      return;
    }

    const existingReview = this.currentUserReview();

    if (existingReview) {
      this.updateReview(existingReview.id, rating);
      return;
    }

    this.createReview(rating);
  }

  private loadReviews(): void {
    this.reviewApiService
      .getByRestaurant(this.restaurantId())
      .subscribe({
        next: summary => {
          this.reviewSummary.set(summary);
        },
        error: () => {
          this.reviewSummary.set(null);
        },
      });
  }

  private loadHasOrdered(): void {
    this.orderApiService
      .hasOrderedFromRestaurant(this.restaurantId())
      .subscribe({
        next: result => {
          this.hasOrdered.set(result.hasOrdered);
        },
        error: () => {
          this.hasOrdered.set(false);
        },
      });
  }

  private loadCurrentUserReview(): void {
    this.reviewApiService
      .getMyReviewByRestaurant(this.restaurantId())
      .subscribe({
        next: review => {
          this.currentUserReview.set(review);
          this.reviewRating.set(review?.rating ?? 0);
        },
        error: () => {
          this.currentUserReview.set(null);
          this.reviewRating.set(0);
        },
      });
  }

  private createReview(rating: number): void {
    this.reviewApiService
      .create({
        restaurantId: this.restaurantId(),
        rating,
      })
      .subscribe({
        next: review => {
          this.handleSavedReview(review, 'Review added.');
        },
        error: () => {
          this.reviewMessage.set('Could not add review.');
        },
      });
  }

  private updateReview(reviewId: number, rating: number): void {
    this.reviewApiService
      .update(reviewId, { rating })
      .subscribe({
        next: review => {
          this.handleSavedReview(review, 'Review updated.');
        },
        error: () => {
          this.reviewMessage.set('Could not update review.');
        },
      });
  }

  private handleSavedReview(
    review: ReviewDto,
    message: string
  ): void {
    this.currentUserReview.set(review);
    this.reviewRating.set(review.rating);
    this.reviewMessage.set(message);
    this.loadReviews();
  }
}