import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MatIconModule } from '@angular/material/icon';

import { DeliveryType, OrderApiService, OrderDto } from '../../shared/order-api.service';
import { OrderStatus } from '../../shared/models/status';
import { Router } from '@angular/router';

interface OrderDetails extends OrderDto {}

interface OrderStep {
  label: string;
  status: OrderStatus;
  icon: string;
}

@Component({
  selector: 'app-order-status-view',
  imports: [DatePipe, DecimalPipe, MatIconModule, TranslocoModule],
  templateUrl: './order-status-view.html',
  styleUrl: './order-status-view.css',
})
export class OrderStatusView implements OnInit {
  private readonly orderApiService = inject(OrderApiService);
  private readonly transloco = inject(TranslocoService);
  private readonly router = inject(Router);
  readonly OrderStatus = OrderStatus;
  readonly DeliveryType = DeliveryType;

  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly order = signal<OrderDetails | null>(null);
  readonly noActiveOrder = signal(false);

ngOnInit(): void {
  this.orderApiService.getCustomerActiveOrder().subscribe({
    next: (activeOrder) => {
      if (!activeOrder) {
        this.noActiveOrder.set(true);
        this.isLoading.set(false);
        return;
      }

      if (activeOrder.status === OrderStatus.AwaitingPayment) {
        this.router.navigate(['/payment', activeOrder.id]);
        return;
      }

      this.order.set(activeOrder);
      this.noActiveOrder.set(false);
      this.isLoading.set(false);
    },
    error: () => {
      this.loadError.set(this.t('orderStatus.error.loadFailed'));
      this.isLoading.set(false);
    },
  });
}

  get steps(): OrderStep[] {
    const isPickup = this.order()?.deliveryType === DeliveryType.Pickup;

    if (isPickup) {
      return [
        { label: this.t('orderStatus.step.placed'), status: OrderStatus.Pending, icon: 'check' },
        { label: this.t('orderStatus.step.preparing'), status: OrderStatus.Preparing, icon: 'restaurant' },
        { label: this.t('orderStatus.step.readyForPickup'), status: OrderStatus.ReadyForPickup, icon: 'store' },
        { label: this.t('orderStatus.step.completed'), status: OrderStatus.Delivered, icon: 'task_alt' },
      ];
    }

    return [
      { label: this.t('orderStatus.step.placed'), status: OrderStatus.Pending, icon: 'check' },
      { label: this.t('orderStatus.step.preparing'), status: OrderStatus.Preparing, icon: 'restaurant' },
      { label: this.t('orderStatus.step.onTheWay'), status: OrderStatus.OnTheWay, icon: 'two_wheeler' },
      { label: this.t('orderStatus.step.delivered'), status: OrderStatus.Delivered, icon: 'home' },
    ];
  }

  get currentStepIndex(): number {
    const currentOrder = this.order();

    if (!currentOrder) {
      return -1;
    }

    return this.steps.findIndex((step) => step.status === currentOrder.status);
  }

  isCompleted(index: number): boolean {
    return index < this.currentStepIndex;
  }

  isActive(index: number): boolean {
    return index === this.currentStepIndex;
  }

  isPending(index: number): boolean {
    return index > this.currentStepIndex;
  }

  get statusTitle(): string {
    switch (this.order()?.status) {
      case OrderStatus.Pending:
        return this.t('orderStatus.status.pending');
      case OrderStatus.Preparing:
        return this.t('orderStatus.status.preparing');
      case OrderStatus.OnTheWay:
        return this.t('orderStatus.status.onTheWay');
      case OrderStatus.ReadyForPickup:
        return this.t('orderStatus.status.readyForPickup');
      case OrderStatus.Delivered:
        return this.t('orderStatus.status.delivered');
      case OrderStatus.Cancelled:
        return this.t('orderStatus.status.cancelled');
      default:
        return this.t('orderStatus.status.pending');
    }
  }

  get statusMessage(): string {
    switch (this.order()?.status) {
      case OrderStatus.Pending:
        return this.t('orderStatus.message.pending');
      case OrderStatus.Preparing:
        return this.t('orderStatus.message.preparing');
      case OrderStatus.OnTheWay:
        return this.t('orderStatus.message.onTheWay');
      case OrderStatus.ReadyForPickup:
        return this.t('orderStatus.message.readyForPickup');
      case OrderStatus.Delivered:
        return this.t('orderStatus.message.delivered');
      case OrderStatus.Cancelled:
        return this.t('orderStatus.message.cancelled');
      default:
        return this.t('orderStatus.message.waiting');
    }
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}