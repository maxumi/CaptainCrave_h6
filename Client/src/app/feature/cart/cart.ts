import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService, CartItem } from '../../shared/cart.service';
import { AuthService } from '../../core/auth/auth.service';
import { CreateOrderRequest, DeliveryType, OrderApiService } from '../../shared/order-api.service';
import { finalize } from 'rxjs';
import { Role } from '../../shared/models/user';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LocationService } from '../../shared/LocationService';
import { OrderStatus } from '../../shared/models/status';

/**
 * Cart component for managing the user's shopping cart.
 *
 * Allows users to view, update, and remove items from their cart, and proceed to checkout/Payment.
 */
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
  private readonly locationService = inject(LocationService);
  private readonly transloco = inject(TranslocoService);
  private readonly router = inject(Router);

  readonly DeliveryType = DeliveryType;

  // Signals and computed properties for the cart component
  readonly selectedDeliveryType = signal<DeliveryType>(DeliveryType.Delivery);
  readonly cartItems = this.cartService.items;
  readonly total = this.cartService.total;
  readonly isSubmittingOrder = signal(false);
  readonly checkoutError = signal<string | null>(null);
  readonly canCheckout = computed(() => this.authService.user()?.role === Role.Customer);
  readonly selectedLocation = this.locationService.selectedLocation;

  ngOnInit(): void {
    this.orderApiService.getCustomerActiveOrder().subscribe(order => {
      if (!order) {
        return;
      }

      if (order.status === OrderStatus.AwaitingPayment) {
        this.router.navigate(['/payment', order.id]);
        return;
      }

      this.router.navigate(['/order-status']);
    });
  }

  // Event handler for when the quantity input changes
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

  // Event handler for when the checkout button is clicked
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
  const selectedLocation = this.selectedLocation();

  const deliveryAddress =
    selectedDeliveryType === DeliveryType.Delivery
      ? selectedLocation?.label?.trim()
      : undefined;

  if (selectedDeliveryType === DeliveryType.Delivery && !deliveryAddress) {
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
    ...(deliveryAddress ? { deliveryAddress } : {}),
  };

  this.isSubmittingOrder.set(true);

  this.orderApiService
    .createOrder(payload)
    .pipe(finalize(() => this.isSubmittingOrder.set(false)))
    .subscribe({
      next: (order) => {
        this.router.navigate(['/payment', order.id]);
      },
      error: (error: HttpErrorResponse) => {
        const backendMessage =
          typeof error.error?.message === 'string'
            ? error.error.message
            : null;

        this.checkoutError.set(
          backendMessage ?? this.t('cart.error.checkoutFailed')
        );
      },
    });
}

private t(key: string): string {
  return this.transloco.translate(key);
}
}
