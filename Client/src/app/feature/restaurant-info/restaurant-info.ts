import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MenuItemApiService, MenuItemDto } from '../../shared/menu-item-api.service';
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
  private readonly menuItemApiService = inject(MenuItemApiService);
  private readonly cartService = inject(CartService);
  private readonly authService = inject(AuthService);
  private readonly transloco = inject(TranslocoService);

  readonly menuItems = signal<MenuItemDto[]>([]);
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

    this.menuItemApiService.getByRestaurantId(restaurantId).subscribe({
      next: (items) => {
        this.menuItems.set(items);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadError.set(this.t('restaurantInfo.error.loadFailed'));
        this.isLoading.set(false);
      },
    });
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

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}