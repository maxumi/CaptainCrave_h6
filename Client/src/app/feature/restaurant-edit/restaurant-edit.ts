import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { EditOrder } from './edit-order/edit-order';
import { EditMenu } from './edit-menu/edit-menu';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/user';
import {
  RestaurantApiService,
  RestaurantDto,
  UpdateRestaurantRequest,
} from '../../shared/restaurant-api.service';

interface RestaurantDraft {
  name: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  isActive: boolean;
}

@Component({
  selector: 'app-restaurant-edit',
  imports: [FormsModule, EditMenu, EditOrder],
  templateUrl: './restaurant-edit.html',
  styleUrl: './restaurant-edit.css',
})
export class RestaurantEdit implements OnInit {
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isAdmin = computed(() => this.authService.user()?.role === Role.Admin);
  activeTab: 'orders' | 'menu' = 'menu';
  readonly currentRestaurant = signal<RestaurantDto | null>(null);
  restaurantDraft: RestaurantDraft = this.createEmptyDraft();
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly selectedCover = signal<File | null>(null);
  readonly isUploading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal<string | null>(null);
  readonly uploadError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadRestaurantContext();
  }

  loadRestaurantContext(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const idParam = this.toRestaurantId(this.route.snapshot.paramMap.get('id'));

    // Prevent default behavior for admin users trying to access restaurant's edit page without specifying a restaurant ID in the URL
    if (this.isAdmin()) {
      if (!idParam) {
        this.loadError.set('Choose a restaurant from search to edit it.');
        void this.router.navigate(['/restaurants-search']);
        this.isLoading.set(false);
        return;
      }

      this.restaurantApiService.getById(idParam).pipe(finalize(() => this.isLoading.set(false))).subscribe({
        next: (restaurant) => this.setRestaurant(restaurant),
        error: () => {
          this.loadError.set('Could not load the selected restaurant.');
          void this.router.navigate(['/restaurants-search']);
        },
      });
      return;
    }

    this.restaurantApiService.getMyRestaurant().pipe(finalize(() => this.isLoading.set(false))).subscribe({
      next: (restaurant) => this.setRestaurant(restaurant),
      error: () => this.loadError.set('Could not load restaurant profile.'),
    });
  }

  saveRestaurant(): void {
    const restaurant = this.currentRestaurant();

    if (!restaurant) {
      return;
    }

    this.isSaving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(null);

    const payload: UpdateRestaurantRequest = { ...this.restaurantDraft };

    this.restaurantApiService
      .updateRestaurant(restaurant.id, payload)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (updated) => {
          this.setRestaurant(updated);
          this.saveSuccess.set('Restaurant updated.');
        },
        error: () => {
          this.saveError.set('Failed to save restaurant changes.');
        },
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
          this.setRestaurant(updated);
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

  private setRestaurant(restaurant: RestaurantDto): void {
    this.currentRestaurant.set(restaurant);
    this.restaurantDraft = this.toDraft(restaurant);
    this.selectedCover.set(null);
    this.uploadError.set(null);
    this.saveError.set(null);
    this.saveSuccess.set(null);
  }

  private toRestaurantId(value: string | null): number | null {
    const parsed = Number(value);

    if (!Number.isInteger(parsed) || parsed <= 0) {
      return null;
    }

    return parsed;
  }
}