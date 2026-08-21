import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { form, FormField, FormRoot } from '@angular/forms/signals';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { catchError, EMPTY, finalize, firstValueFrom, map, of, tap } from 'rxjs';

import {
  DeliveryType,
  OrderApiService,
  OrderDto,
  UpdateOrderStatusRequest,
} from '../../../shared/order-api.service';
import { OrderStatus } from '../../../shared/models/status';

interface OrderDraft {
  status: OrderStatus;
}

type OrderMode = 'active' | 'history';

@Component({
  selector: 'app-edit-order',
  imports: [TranslocoModule, DatePipe, FormField, FormRoot],
  templateUrl: './edit-order.html',
  styleUrl: './edit-order.css',
})
export class EditOrder {
  private readonly orderApiService = inject(OrderApiService);
  private readonly transloco = inject(TranslocoService);

  readonly OrderStatus = OrderStatus;

  // get the restaurantId from the parent component to use in the API calls
  readonly restaurantId = input<number | null>(null);

  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal<string | null>(null);
  readonly mode = signal<OrderMode>('active');
  readonly activeOrders = signal<OrderDto[]>([]);
  readonly historicOrders = signal<OrderDto[]>([]);
  readonly orders = computed(() =>
    this.mode() === 'active' ? this.activeOrders() : this.historicOrders()
  );
  readonly selectedOrder = signal<OrderDto | null>(null);

  readonly isSelectedOrderHistoric = computed(() => {
    const order = this.selectedOrder();

    if (!order) {
      return false;
    }

    return order.status === OrderStatus.Delivered || order.status === OrderStatus.Cancelled;
  });

  readonly selectableStatuses = computed(() => {
    const order = this.selectedOrder();

    if (!order || this.isSelectedOrderHistoric()) {
      return [] as OrderStatus[];
    }

    return [order.status, ...this.getNextAllowedStatuses(order)];
  });

  readonly orderModel = signal<OrderDraft>({
    status: OrderStatus.Pending,
  });

  readonly orderForm = form(this.orderModel, {
    submission: {
      action: () => this.saveOrder(),
    },
  });

  ngOnInit(): void {
    const restaurantId = this.restaurantId();

    if (restaurantId == null) {
      this.loadError.set('Missing restaurant id.');
      this.isLoading.set(false);
      return;
    }

    this.loadOrders('active', restaurantId);
  }

  setMode(mode: OrderMode): void {
    if (this.mode() === mode) {
      return;
    }

    this.mode.set(mode);
    this.selectedOrder.set(null);
    this.orderModel.set({ status: OrderStatus.Pending });
    this.saveError.set(null);
    this.saveSuccess.set(null);

    const hasData = mode === 'active'
      ? this.activeOrders().length > 0
      : this.historicOrders().length > 0;

    if (!hasData) {
      const restaurantId = this.restaurantId();

      if (restaurantId != null) {
        this.loadOrders(mode, restaurantId);
      }
    }
  }

  saveOrder(): Promise<null | { kind: 'serverError'; message: string }> {
    const selectedOrder = this.selectedOrder();

    if (!selectedOrder || this.isSelectedOrderHistoric()) {
      return Promise.resolve(null);
    }

    const nextAllowed = this.getNextAllowedStatuses(selectedOrder);
    const targetStatus = this.orderModel().status;

    if (targetStatus === selectedOrder.status || !nextAllowed.includes(targetStatus)) {
      const message = this.t('orderManage.error.invalidNextStatus');
      this.saveError.set(message);

      return Promise.resolve({ kind: 'serverError', message });
    }

    this.saveError.set(null);
    this.saveSuccess.set(null);

    return firstValueFrom(
      this.orderApiService.updateOrderStatus(selectedOrder.id, {
        status: targetStatus,
      } satisfies UpdateOrderStatusRequest).pipe(
        tap(updated => {
          this.selectedOrder.set(updated);

          if (updated.status === OrderStatus.Delivered || updated.status === OrderStatus.Cancelled) {
            this.activeOrders.set(this.activeOrders().filter(order => order.id !== updated.id));
            this.historicOrders.set([updated, ...this.historicOrders()]);
            this.selectedOrder.set(null);
            this.orderModel.set({ status: OrderStatus.Pending });
          } else {
            this.activeOrders.set(
              this.activeOrders().map(order => (order.id === updated.id ? updated : order))
            );
            this.orderModel.set({
              status: this.getNextAllowedStatuses(updated)[0] ?? updated.status,
            });
          }

          this.saveSuccess.set(this.t('orderManage.success.updated'));
        }),

        map(() => null),

        catchError((error: HttpErrorResponse) => {
          const backendMessage = typeof error.error?.message === 'string'
            ? error.error.message
            : null;

          const message = backendMessage ?? this.t('orderManage.error.updateFailed');
          this.saveError.set(message);

          return of({
            kind: 'serverError' as const,
            message,
          });
        }),
      ),
    );
  }

  selectOrder(order: OrderDto): void {
    const currentOrder = this.selectedOrder();

    if (currentOrder?.id === order.id) {
      this.selectedOrder.set(null);
      this.orderModel.set({
        status: OrderStatus.Pending,
      });
    } else {
      this.selectedOrder.set(order);
      this.orderModel.set({
        status: this.getNextAllowedStatuses(order)[0] ?? order.status,
      });
    }

    this.saveError.set(null);
    this.saveSuccess.set(null);
  }

private loadOrders(mode: OrderMode, restaurantId: number): void {
  this.isLoading.set(true);
  this.loadError.set(null);
  this.saveError.set(null);
  this.saveSuccess.set(null);

  const request$ = mode === 'active'
    ? this.orderApiService.getRestaurantActiveOrdersByRestaurantId(restaurantId)
    : this.orderApiService.getRestaurantHistoricOrdersByRestaurantId(restaurantId);

  request$.pipe(
    catchError((error: { status?: number }) => {
      if (error.status === 401 || error.status === 403) {
        this.loadError.set(this.t('orderManage.error.notAllowed'));
      } else if (error.status === 404) {
        this.loadError.set(this.t('orderManage.error.notFound'));
      } else {
        this.loadError.set(this.t('orderManage.error.loadFailed'));
      }

      if (mode === 'active') {
        this.activeOrders.set([]);
      } else {
        this.historicOrders.set([]);
      }

      this.selectedOrder.set(null);

      return EMPTY;
    }),
    finalize(() => {
      this.isLoading.set(false);
    }),
  ).subscribe((orders) => {
    if (mode === 'active') {
      this.activeOrders.set(orders);
    } else {
      this.historicOrders.set(orders);
    }

    if (orders.length > 0) {
      this.selectOrder(orders[0]);
    } else {
      this.selectedOrder.set(null);
      this.orderModel.set({
        status: OrderStatus.Pending,
      });
    }
  });
}

  private getNextAllowedStatuses(order: OrderDto): OrderStatus[] {
    if (order.status === OrderStatus.Delivered || order.status === OrderStatus.Cancelled) {
      return [];
    }

    if (order.deliveryType === DeliveryType.Delivery) {
      switch (order.status) {
        case OrderStatus.Pending:
          return [OrderStatus.Preparing, OrderStatus.Cancelled];
        case OrderStatus.Preparing:
          return [OrderStatus.OnTheWay, OrderStatus.Cancelled];
        case OrderStatus.OnTheWay:
          return [OrderStatus.Delivered, OrderStatus.Cancelled];
        default:
          return [];
      }
    }

    switch (order.status) {
      case OrderStatus.Pending:
        return [OrderStatus.Preparing, OrderStatus.Cancelled];
      case OrderStatus.Preparing:
        return [OrderStatus.ReadyForPickup, OrderStatus.Cancelled];
      case OrderStatus.ReadyForPickup:
        return [OrderStatus.Delivered, OrderStatus.Cancelled];
      default:
        return [];
    }
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}