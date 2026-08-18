import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { CommonModule } from '@angular/common';

import { OrderApiService, OrderDto } from '../../shared/order-api.service';

@Component({
  selector: 'app-order-history',
  imports: [DatePipe, DecimalPipe, TranslocoModule, CommonModule],
  templateUrl: './order-history.html',
  styleUrl: './order-history.css',
})
export class OrderHistory implements OnInit {
  private readonly orderApiService = inject(OrderApiService);
  private readonly transloco = inject(TranslocoService);

  readonly orders = signal<OrderDto[]>([]);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.orderApiService.getCustomerHistoricOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders ?? []);
        this.isLoading.set(false);
      },
      error: () => {
        this.orders.set([]);
        this.loadError.set(this.t('orderHistory.loadError'));
        this.isLoading.set(false);
      },
    });
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}
