import { inject, Service } from '@angular/core';
import { PaymentDto, CreatePaymentRequest } from './models/payment';
import { HttpClient } from '@angular/common/http';

@Service()
export class PaymentApiService {
  private readonly http = inject(HttpClient);

  createPayment(payload: CreatePaymentRequest) {
    return this.http.post<PaymentDto>('/api/payments', payload);
  }
}