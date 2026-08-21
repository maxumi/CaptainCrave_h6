import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';

import { EditOrder } from './edit-order/edit-order';
import { EditMenu } from './edit-menu/edit-menu';
import { EditRestaurantInfo } from './edit-restaurant-info/edit-restaurant-info';

import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/user';
import {
  RestaurantApiService,
  RestaurantDto,
} from '../../shared/restaurant-api.service';

@Component({
  selector: 'app-restaurant-edit',
  imports: [EditMenu, EditOrder, EditRestaurantInfo],
  templateUrl: './restaurant-edit.html',
  styleUrl: './restaurant-edit.css',
})
export class RestaurantEdit implements OnInit {
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isAdmin = computed(
    () => this.authService.user()?.role === Role.Admin
  );

  readonly currentRestaurant = signal<RestaurantDto | null>(null);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);

  activeTab: 'restaurant' | 'orders' | 'menu' = 'restaurant';

  ngOnInit(): void {
    this.loadRestaurantContext();
  }

  loadRestaurantContext(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const idParam = this.toRestaurantId(
      this.route.snapshot.paramMap.get('id')
    );

    if (this.isAdmin()) {
      if (!idParam) {
        this.loadError.set(
          'Choose a restaurant from search to edit it.'
        );

        void this.router.navigate(['/restaurants-search']);
        this.isLoading.set(false);
        return;
      }

      this.restaurantApiService
        .getById(idParam)
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: (restaurant) => {
            this.currentRestaurant.set(restaurant);
          },
          error: () => {
            this.loadError.set(
              'Could not load the selected restaurant.'
            );

            void this.router.navigate(['/restaurants-search']);
          },
        });

      return;
    }

    this.restaurantApiService
      .getMyRestaurant()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (restaurant) => {
          this.currentRestaurant.set(restaurant);
        },
        error: () => {
          this.loadError.set(
            'Could not load restaurant profile.'
          );
        },
      });
  }

  private toRestaurantId(value: string | null): number | null {
    const parsed = Number(value);

    return Number.isInteger(parsed) && parsed > 0
      ? parsed
      : null;
  }
}