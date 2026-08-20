import { Component, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { EditOrder } from './edit-order/edit-order';
import { EditMenu } from './edit-menu/edit-menu';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';

@Component({
  selector: 'app-restaurant-edit',
  imports: [EditMenu, EditOrder],
  templateUrl: './restaurant-edit.html',
  styleUrl: './restaurant-edit.css',
})
export class RestaurantEdit implements OnInit {
  private readonly restaurantApiService = inject(RestaurantApiService);

  activeTab: 'orders' | 'menu' = 'menu';
  readonly currentRestaurant = signal<RestaurantDto | null>(null);
  readonly selectedCover = signal<File | null>(null);
  readonly isUploading = signal(false);
  readonly uploadError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadRestaurant();
  }

  loadRestaurant(): void {
    this.restaurantApiService.getMyRestaurant().subscribe({
      next: (restaurant) => this.currentRestaurant.set(restaurant),
      error: () => this.uploadError.set('Could not load restaurant profile.'),
    });
  }

  onCoverSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedCover.set(file);
  }

  uploadCover(): void {
    const restaurant = this.currentRestaurant();
    const file = this.selectedCover();

    if (!restaurant || !file) {
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set(null);

    this.restaurantApiService
      .uploadImage(restaurant.id, file)
      .pipe(finalize(() => this.isUploading.set(false)))
      .subscribe({
        next: (updated) => {
          this.currentRestaurant.set(updated);
          this.selectedCover.set(null);

          const input = document.getElementById('restaurant-cover-input') as HTMLInputElement | null;
          if (input) {
            input.value = '';
          }
        },
        error: () => {
          this.uploadError.set('Failed to upload restaurant cover image.');
        },
      });
  }
}