import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MenuItemApiService, MenuItemDto } from '../../shared/menu-item-api.service';
import { MenuApiService, MenuDto } from '../../shared/menu-api.service';
import { CategoryApiService, CategoryDto } from '../../shared/category-api.service';
import { CartService } from '../../shared/cart.service';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/user';

@Component({
  selector: 'app-restaurant-info',
  imports: [TranslocoModule],
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
  readonly canAddToCart = computed(() => this.authService.user()?.role === Role.Customer);

  private restaurantId = 0;

  ngOnInit(): void {
    const restaurantId = Number(this.route.snapshot.queryParamMap.get('restaurantId'));

    if (!Number.isFinite(restaurantId) || restaurantId <= 0) {
      this.loadError.set(this.t('restaurantInfo.error.missingRestaurantId'));
      this.isLoading.set(false);
      return;
    }

    this.restaurantId = restaurantId;

    this.menuApiService.getByRestaurantId(restaurantId).subscribe({
      next: (menus) => {
        this.menus.set(menus);

        if (!menus.length) {
          this.menuItems.set([]);
          this.isLoading.set(false);
          return;
        }

        const firstMenuId = menus[0].id;
        this.selectedMenuId.set(firstMenuId);
        this.loadMenuData(firstMenuId);
      },
      error: () => {
        this.loadError.set(this.t('restaurantInfo.error.loadFailed'));
        this.isLoading.set(false);
      },
    });
  }

  selectMenu(menuId: number): void {
    this.selectedMenuId.set(menuId);
    this.loadMenuData(menuId);
  }

  addToCart(item: MenuItemDto): void {
    if (!this.canAddToCart()) {
      this.addMessage.set(this.t('restaurantInfo.error.customerOnly'));
      return;
    }

    const wasAdded = this.cartService.addItem(item, this.restaurantId);

    if (!wasAdded) {
      this.addMessage.set(this.t('restaurantInfo.error.singleRestaurantOnly'));
      return;
    }

    this.addMessage.set(
      this.transloco.translate('restaurantInfo.message.addedToCart', {
        name: item.name,
      })
    );
  }

  private loadMenuData(menuId: number): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.categoryApiService.getByMenuId(menuId).subscribe({
      next: (categories) => {
        this.categories.set(categories);

        this.menuItemApiService.getByMenuId(menuId).subscribe({
          next: (items) => {
            this.menuItems.set(items);
            this.isLoading.set(false);
          },
          error: () => {
            this.loadError.set(this.t('restaurantInfo.error.loadFailed'));
            this.isLoading.set(false);
          },
        });
      },
      error: () => {
        this.loadError.set(this.t('restaurantInfo.error.loadFailed'));
        this.isLoading.set(false);
      },
    });
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}