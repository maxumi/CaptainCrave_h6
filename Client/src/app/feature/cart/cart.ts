import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService, CartItem } from '../../shared/cart.service';
import { AuthService } from '../../core/auth/auth.service';
import { CreateOrderRequest, DeliveryType, OrderApiService } from '../../shared/order-api.service';
import { finalize } from 'rxjs';
import { Role } from '../../shared/models/user';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-cart',
  imports: [TranslocoModule],
  templateUrl: './cart.html',
  styleUrl: './cart.css',
})
export class Cart implements OnInit {
  readonly cartService = inject(CartService);
  private readonly authService = inject(AuthService);
  private readonly orderApiService = inject(OrderApiService);
  private readonly router = inject(Router);
  readonly DeliveryType = DeliveryType;
  readonly selectedDeliveryType = signal<DeliveryType>(DeliveryType.Delivery);
  private readonly transloco = inject(TranslocoService);

  readonly cartItems = this.cartService.items;
  readonly total = this.cartService.total;
  readonly isSubmittingOrder = signal(false);
  readonly checkoutError = signal<string | null>(null);
  readonly canCheckout = computed(() => this.authService.user()?.role === Role.Customer);

  ngOnInit(): void {
    this.orderApiService.getCustomerActiveOrder().subscribe(order => {
      if (order) {
        this.router.navigate(['/order-status']);
      }
    });
  }

  onQuantityInput(item: CartItem, event: Event): void {
    const input = event.target as HTMLInputElement;
    const quantity = Number(input.value);

    if (!Number.isFinite(quantity)) {
      return;
    }

    this.cartService.updateQuantity(item.menuItemId, Math.floor(quantity));
  }

  removeItem(item: CartItem): void {
    this.cartService.removeItem(item.menuItemId);
  }

  clearCart(): void {
    this.cartService.clear();
  }

  checkout(): void {
    const user = this.authService.user();
    const items = this.cartItems();

    this.checkoutError.set(null);

    if (!user) {
      this.checkoutError.set(this.t('cart.error.notLoggedIn'));
      return;
    }

    if (user.role !== Role.Customer) {
      this.checkoutError.set(this.t('cart.error.customerOnly'));
      return;
    }

    if (!items.length) {
      this.checkoutError.set(this.t('cart.error.empty'));
      return;
    }

    const selectedDeliveryType = this.selectedDeliveryType();

    if (
      selectedDeliveryType === DeliveryType.Delivery &&
      (!user.address || user.latitude == null || user.longitude == null)
    ) {
      this.checkoutError.set(this.t('cart.error.addressRequired'));
      return;
    }

    const restaurantId = items[0].restaurantId;

    const payload: CreateOrderRequest = {
      userId: user.userId,
      restaurantId,
      deliveryType: selectedDeliveryType,
      items: items.map((item) => ({
        menuItemId: item.menuItemId,
        quantity: item.quantity,
      })),
    };

    if (selectedDeliveryType === DeliveryType.Delivery) {
      payload.deliveryAddress = user.address;
    }

    this.isSubmittingOrder.set(true);

    this.orderApiService
      .createOrder(payload)
      .pipe(finalize(() => this.isSubmittingOrder.set(false)))
      .subscribe({
        next: () => {
          this.cartService.clear();
          this.router.navigate(['/order-status']);
        },
        error: (error: HttpErrorResponse) => {
          const backendMessage =
            typeof error.error?.message === 'string' ? error.error.message : null;

          this.checkoutError.set(backendMessage ?? this.t('cart.error.checkoutFailed'));
        },
      });
  }
private t(key: string): string {
  return this.transloco.translate(key);
}
}
