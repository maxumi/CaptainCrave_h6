import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MenuItemCard } from './menu-item-card/menu-item-card';
import { ActivatedRoute } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MenuItemApiService, MenuItemDto } from '../../shared/menu-item-api.service';
import { MenuApiService, MenuDto } from '../../shared/menu-api.service';
import { CategoryApiService, CategoryDto } from '../../shared/category-api.service';
import { CartService } from '../../shared/cart.service';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/user';
import { finalize, forkJoin } from 'rxjs';

@Component({
  selector: 'app-restaurant-info',
  imports: [TranslocoModule, MenuItemCard],
  templateUrl: './restaurant-info.html',
  styleUrl: './restaurant-info.css',
})
export class RestaurantInfo implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly menuApiService = inject(MenuApiService);
  private readonly categoryApiService = inject(CategoryApiService);
  private readonly menuItemApiService = inject(MenuItemApiService);
  private readonly cartService = inject(CartService);
  private readonly authService = inject(AuthService);
  private readonly transloco = inject(TranslocoService);

  readonly menus = signal<MenuDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);
  readonly menuItems = signal<MenuItemDto[]>([]);
  readonly selectedMenuId = signal<number | null>(null);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly addMessage = signal<string | null>(null);

  readonly canAddToCart = computed(
    () => this.authService.user()?.role === Role.Customer
  );

  readonly uncategorizedItems = computed(() => {
    // Get all valid category IDs. Set is for unique values.
    const categoryIds = new Set(
      this.categories().map((category) => category.id)
    );

    // Keep items without a valid category.
    return this.menuItems().filter(
      (item) => !item.categoryId || !categoryIds.has(item.categoryId)
    );
  });

  readonly itemsByCategory = computed(() => {
    // Group menu items by category ID. 
    const result = new Map<number, MenuItemDto[]>();

    for (const item of this.menuItems()) {
      if (!item.categoryId) {
        continue;
      }

      const items = result.get(item.categoryId) ?? [];
      items.push(item);
      result.set(item.categoryId, items);
    }

    return result;
  });

  private restaurantId = 0;

  ngOnInit(): void {
    const restaurantId = Number(
      this.route.snapshot.queryParamMap.get('restaurantId')
    );

    // Validate the restaurantId parameter.
    if (!Number.isFinite(restaurantId) || restaurantId <= 0) {
      this.loadError.set(this.t('restaurantInfo.error.missingRestaurantId'));
      this.isLoading.set(false);
      return;
    }

    this.restaurantId = restaurantId;
    this.loadMenus(restaurantId);
  }

  private loadMenus(restaurantId: number): void {
    this.menuApiService.getByRestaurantId(restaurantId).subscribe({
      next: (menus) => {
        this.menus.set(menus);

        const firstMenu = menus[0];

        if (!firstMenu) {
          this.isLoading.set(false);
          return;
        }

        this.selectedMenuId.set(firstMenu.id);
        this.loadMenuData(firstMenu.id);
      },
      error: () => {
        this.loadError.set(this.t('restaurantInfo.error.loadFailed'));
        this.isLoading.set(false);
      },
    });
  }
  selectMenu(menuId: number): void {
    this.selectedMenuId.set(menuId);
    this.addMessage.set(null);
    this.loadMenuData(menuId);
  }

  addToCart(item: MenuItemDto): void {
    if (!this.canAddToCart()) {
      this.addMessage.set(
        this.t('restaurantInfo.error.customerOnly')
      );
      return;
    }

    const wasAdded = this.cartService.addItem(
      item,
      this.restaurantId
    );

    if (!wasAdded) {
      this.addMessage.set(
        this.t('restaurantInfo.error.singleRestaurantOnly')
      );
      return;
    }

    this.addMessage.set(
      this.transloco.translate(
        'restaurantInfo.message.addedToCart',
        {
          name: item.name,
        }
      )
    );
  }

  private loadMenuData(menuId: number): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.categories.set([]);
    this.menuItems.set([]);

    // Use forkJoin to load categories and menu items at the same time.
    forkJoin({
      categories: this.categoryApiService.getByMenuId(menuId),
      items: this.menuItemApiService.getByMenuId(menuId),
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ categories, items }) => {
          this.categories.set(categories);
          this.menuItems.set(items);
        },
        error: () => {
          this.loadError.set(this.t('restaurantInfo.error.loadFailed'));
        },
      });
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}