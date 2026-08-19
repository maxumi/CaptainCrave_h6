import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-restaurants',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './restaurants.html',
  styleUrl: './restaurants.css',
})
export class Restaurants implements OnInit {
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly translocoService = inject(TranslocoService);

  readonly restaurants = signal<RestaurantDto[]>([]);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadRestaurants();
  }

  private loadRestaurants(): void {
    this.isLoading.set(true);

    this.restaurantApiService.getRestaurants().subscribe({
      next: (restaurants) => {
        this.restaurants.set(restaurants);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadError.set(this.t('restaurants.error.loadFailed'));
        this.isLoading.set(false);
      },
    });
  }

  private t(key: string): string {
    return this.translocoService.translate(key);
  }
}
