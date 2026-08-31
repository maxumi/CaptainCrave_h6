import { Component, computed, inject, input, signal } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { RestaurantReviewSummaryDto, ReviewApiService, ReviewDto } from '../../../shared/review-api.service';

@Component({
  selector: 'app-restaurant-reviews',
  imports: [],
  templateUrl: './restaurant-reviews.html',
  styleUrl: './restaurant-reviews.css',
})
export class RestaurantReviews {
  readonly restaurantId = input.required<number>();

  private readonly authService = inject(AuthService);
  private readonly reviewApiService = inject(ReviewApiService);

  readonly reviewSummary =
    signal<RestaurantReviewSummaryDto | null>(null);

  readonly reviewRating = signal(0);
  readonly reviewMessage = signal<string | null>(null);

  readonly currentUserReview = computed<ReviewDto | null>(() => {
    const userId = this.authService.user()?.userId;
    const summary = this.reviewSummary();

    if (!userId || !summary) {
      return null;
    }

    return summary.reviews.find(
      review => review.userId === userId
    ) ?? null;
  });

  ngOnInit(): void {
    this.loadReviews();
  }

  setRating(rating: number): void {
    this.reviewRating.set(rating);
  }

  saveReview(): void {
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

          const userId = this.authService.user()?.userId;

          const existingReview = summary.reviews.find(
            review => review.userId === userId
          );

          this.reviewRating.set(existingReview?.rating ?? 0);
        },
        error: () => {
          this.reviewSummary.set(null);
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
        next: () => {
          this.reviewMessage.set('Review added.');
          this.loadReviews();
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
        next: () => {
          this.reviewMessage.set('Review updated.');
          this.loadReviews();
        },
        error: () => {
          this.reviewMessage.set('Could not update review.');
        },
      });
  }
}