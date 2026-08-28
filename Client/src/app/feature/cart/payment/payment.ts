import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { PaymentApiService } from '../../../shared/payment-api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { CartService } from '../../../shared/cart.service';
import { PaymentStatus } from '../../../shared/models/payment';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-payment',
  imports: [TranslocoModule],
  templateUrl: './payment.html',
  styleUrl: './payment.css',
})
export class Payment {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly paymentApiService = inject(PaymentApiService);
  private readonly cartService = inject(CartService);

  readonly orderId = Number(this.route.snapshot.paramMap.get('orderId'));

  readonly cardNumber = signal('');
  readonly isPaying = signal(false);
  readonly paymentError = signal<string | null>(null);

  onCardNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.cardNumber.set(input.value);
  }

  pay(): void {
    const cardNumber = this.cardNumber().trim();

    this.paymentError.set(null);

if (!this.orderId) {
  this.paymentError.set(this.t('payment.error.invalidOrder'));
  return;
}

if (cardNumber.length < 4) {
  this.paymentError.set(this.t('payment.error.cardNumberTooShort'));
  return;
}

    this.isPaying.set(true);

    this.paymentApiService
      .createPayment({
        orderId: this.orderId,
        cardNumber,
      })
      .pipe(finalize(() => this.isPaying.set(false)))
      .subscribe({
        next: (payment) => {
          if (payment.status === PaymentStatus.Succeeded) {
            this.cartService.clear();
            this.router.navigate(['/order-status']);
            return;
          }

          this.paymentError.set(this.t('payment.error.failed'));
        },
        error: (error: HttpErrorResponse) => {
          const backendMessage =
            typeof error.error?.message === 'string'
              ? error.error.message
              : null;

          this.paymentError.set(
            backendMessage ?? this.t('payment.error.failed')
          );
        },
      });
  }

  t(key: string): string {
    return this.transloco.translate(key);
  }
}