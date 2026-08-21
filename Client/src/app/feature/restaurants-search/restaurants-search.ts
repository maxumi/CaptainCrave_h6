import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/user';

@Component({
  selector: 'app-restaurants-search',
  imports: [TranslocoModule, RouterLink, FormsModule],
  templateUrl: 'restaurants-search.html',
  styleUrl: 'restaurants-search.css',
})
export class RestaurantsSearch implements OnInit {
  private readonly pageSize = 10;

  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly transloco = inject(TranslocoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  readonly restaurants = signal<RestaurantDto[]>([]);
  readonly query = signal('');
  readonly page = signal(1);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly isAdmin = computed(() => this.authService.user()?.role === Role.Admin);

  readonly nameFilteredRestaurants = computed(() => {
    const query = this.normalize(this.query());
    const restaurants = this.restaurants();

    if (!query) {
      return restaurants;
    }

    return restaurants.filter((restaurant) =>
      this.normalize(restaurant.name).includes(query)
    );
  });

  readonly totalItems = computed(() => this.nameFilteredRestaurants().length);

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalItems() / this.pageSize)) 
  );

  readonly pagedRestaurants = computed(() => {
    const currentPage = Math.min(this.page(), this.totalPages());
    const start = (currentPage - 1) * this.pageSize;

    return this.nameFilteredRestaurants().slice(
      start,
      start + this.pageSize
    );
  });

  readonly hasPreviousPage = computed(() => this.page() > 1);
  readonly hasNextPage = computed(() => this.page() < this.totalPages());

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.query.set((params.get('query') ?? '').trim());
      this.page.set(this.toPage(params.get('page')));

      if (this.page() > this.totalPages()) {
        this.goToPage(this.totalPages());
      }
    });

    this.restaurantApiService.getRestaurants().subscribe({
      next: (restaurants) => {
        this.restaurants.set(restaurants);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadError.set(this.t('restaurantsSearch.error.loadFailed'));
        this.isLoading.set(false);
      },
    });
  }

  applyQuery(): void {
    const value = this.query().trim();

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        query: value || null,
        page: 1,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  goToPreviousPage(): void {
    if (!this.hasPreviousPage()) {
      return;
    }

    this.goToPage(this.page() - 1);
  }

  goToNextPage(): void {
    if (!this.hasNextPage()) {
      return;
    }

    this.goToPage(this.page() + 1);
  }

  private goToPage(page: number): void {
    const safePage = Math.max(1, page);
    const value = this.query().trim();

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        query: value || null,
        page: safePage,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  clearQuery(): void {
    this.query.set('');
    this.applyQuery();
  }

  private normalize(value: string | null | undefined): string {
    return (value ?? '').trim().toLowerCase();
  }

  private toPage(value: string | null): number {
    const parsed = Number(value);

    if (!Number.isInteger(parsed) || parsed < 1) {
      return 1;
    }

    return parsed;
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}