import {
  Component,
  OnInit,
  computed,
  inject,
  signal,
  DestroyRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import {
  RestaurantApiService,
  RestaurantDto,
} from '../../shared/restaurant-api.service';

@Component({
  selector: 'app-restaurants-search',
  imports: [TranslocoModule, RouterLink, FormsModule],
  templateUrl: 'restaurants-search.html',
  styleUrl: 'restaurants-search.css',
})
export class RestaurantsSearch implements OnInit {
  private readonly pageSize = 10;

  private readonly restaurantApi = inject(RestaurantApiService);
  private readonly transloco = inject(TranslocoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly restaurants = signal<RestaurantDto[]>([]);
  readonly query = signal('');
  readonly page = signal(1);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly filteredRestaurants = computed(() => {
    const query = this.normalize(this.query());

    if (!query) {
      return this.restaurants();
    }

    return this.restaurants().filter((restaurant) =>
      this.normalize(restaurant.name).includes(query)
    );
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredRestaurants().length / this.pageSize))
  );

  readonly pagedRestaurants = computed(() => {
    const start = (this.page() - 1) * this.pageSize;

    return this.filteredRestaurants().slice(start, start + this.pageSize);
  });

  readonly hasPreviousPage = computed(() => this.page() > 1);
  readonly hasNextPage = computed(() => this.page() < this.totalPages());

  ngOnInit(): void {
    this.readQueryParams();
    this.loadRestaurants();
  }

  applyQuery(): void {
    this.navigate({
      query: this.query().trim() || null,
      page: 1,
    });
  }

  clearQuery(): void {
    this.query.set('');
    this.applyQuery();
  }

  goToPreviousPage(): void {
    if (this.hasPreviousPage()) {
      this.goToPage(this.page() - 1);
    }
  }

  goToNextPage(): void {
    if (this.hasNextPage()) {
      this.goToPage(this.page() + 1);
    }
  }

  private readQueryParams(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.query.set((params.get('query') ?? '').trim());
        this.page.set(this.toPage(params.get('page')));
      });
  }

  private loadRestaurants(): void {
    this.restaurantApi
      .getRestaurants()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (restaurants) => {
          this.restaurants.set(restaurants);
          this.isLoading.set(false);

          if (this.page() > this.totalPages()) {
            this.goToPage(this.totalPages());
          }
        },
        error: () => {
          this.loadError.set(
            this.transloco.translate('restaurantsSearch.error.loadFailed')
          );
          this.isLoading.set(false);
        },
      });
  }

  private goToPage(page: number): void {
    this.navigate({
      query: this.query().trim() || null,
      page: Math.max(1, page),
    });
  }

  private navigate(queryParams: {
    query: string | null;
    page: number;
  }): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private normalize(value: string | null | undefined): string {
    return (value ?? '').trim().toLowerCase();
  }

  private toPage(value: string | null): number {
    const page = Number(value);
    return Number.isInteger(page) && page >= 1 ? page : 1;
  }
}