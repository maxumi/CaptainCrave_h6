import {
  Component,
  input,
  signal,
  effect,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import {
  RestaurantApiService,
  RestaurantDto,
  UpdateRestaurantRequest,
} from '../../../shared/restaurant-api.service';

interface RestaurantDraft {
  name: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  isActive: boolean;
}

@Component({
  selector: 'app-edit-restaurant-info',
  imports: [FormsModule],
  templateUrl: './edit-restaurant-info.html',
  styleUrl: './edit-restaurant-info.css',
})
export class EditRestaurantInfo {
  private readonly restaurantApi = inject(RestaurantApiService);

  readonly restaurant = input.required<RestaurantDto>();

  restaurantDraft: RestaurantDraft = this.createEmptyDraft();

  readonly isSaving = signal(false);
  readonly selectedCover = signal<File | null>(null);
  readonly isUploading = signal(false);

  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal<string | null>(null);
  readonly uploadError = signal<string | null>(null);

  constructor() {
    effect(() => {
      this.restaurantDraft = this.toDraft(this.restaurant());
    });
  }

  saveRestaurant(): void {
    const restaurant = this.restaurant();

    this.isSaving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(null);

    const payload: UpdateRestaurantRequest = {
      ...this.restaurantDraft,
    };

    this.restaurantApi
      .updateRestaurant(restaurant.id, payload)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (updated) => {
          this.restaurantDraft = this.toDraft(updated);
          this.saveSuccess.set('Restaurant updated.');
        },
        error: () => {
          this.saveError.set('Failed to save restaurant changes.');
        },
      });
  }

  onCoverSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedCover.set(input.files?.[0] ?? null);
  }

  uploadCover(): void {
    const file = this.selectedCover();

    if (!file) {
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set(null);

    this.restaurantApi
      .uploadImage(this.restaurant().id, file)
      .pipe(finalize(() => this.isUploading.set(false)))
      .subscribe({
        next: () => {
          this.selectedCover.set(null);
        },
        error: () => {
          this.uploadError.set(
            'Failed to upload restaurant cover image.'
          );
        },
      });
  }

  private createEmptyDraft(): RestaurantDraft {
    return {
      name: '',
      description: '',
      address: '',
      latitude: 0,
      longitude: 0,
      isActive: true,
    };
  }

  private toDraft(restaurant: RestaurantDto): RestaurantDraft {
    return {
      name: restaurant.name,
      description: restaurant.description,
      address: restaurant.address,
      latitude: restaurant.latitude,
      longitude: restaurant.longitude,
      isActive: restaurant.isActive,
    };
  }
}