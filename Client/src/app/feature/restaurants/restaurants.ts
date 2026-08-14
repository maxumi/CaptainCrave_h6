import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
const MAX_DISTANCE_KM = 50;

@Component({
  selector: 'app-restaurants',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './restaurants.html',
  styleUrl: './restaurants.css',
})
export class Restaurants implements OnInit {
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly authService = inject(AuthService);
  private readonly translocoService = inject(TranslocoService);

  readonly restaurants = signal<RestaurantDto[]>([]);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

ngOnInit(): void {
  const user = this.authService.user();

  if (!user) {
    this.loadError.set(this.t('restaurants.error.notAuthenticated'));
    this.isLoading.set(false);
    return;
  }

  if (user.role.toLowerCase() === 'restaurant') {
    this.restaurantApiService.getMyRestaurant().subscribe({
      next: (restaurant) => {
        if (restaurant.latitude == null || restaurant.longitude == null) {
          this.loadError.set(this.t('restaurants.error.restaurantLocationMissing'));
          this.isLoading.set(false);
          return;
        }

        this.loadNearbyRestaurants(restaurant.latitude, restaurant.longitude);
      },
      error: () => {
        this.loadError.set(this.t('restaurants.error.restaurantLocationFailed'));
        this.isLoading.set(false);
      },
    });

    return;
  }

  if (user.latitude == null || user.longitude == null) {
    this.loadError.set(this.t('restaurants.error.userLocationMissing'));
    this.isLoading.set(false);
    return;
  }

  this.loadNearbyRestaurants(user.latitude, user.longitude);
}


private loadNearbyRestaurants(latitude: number, longitude: number): void {
  this.restaurantApiService.getNearbyRestaurants(latitude, longitude, MAX_DISTANCE_KM).subscribe({
    next: (restaurants) => {
      this.restaurants.set(restaurants);
      this.isLoading.set(false);
    },
    error: () => {
      this.loadError.set('Unable to load restaurants right now.');
      this.isLoading.set(false);
    },
  });
}
private t(key: string): string {
  return this.translocoService.translate(key);
}
}
