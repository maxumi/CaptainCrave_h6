// Payment-related models and enums.
export enum PaymentStatus {
  Pending = 'pending',
  Succeeded = 'succeeded',
  Failed = 'failed',
  Cancelled = 'cancelled',
  Refunded = 'refunded',
}

export interface CreatePaymentRequest {
  orderId: number;
  cardNumber: string;
}

export interface PaymentDto {
  id: number;
  orderId: number;
  amount: number;
  status: PaymentStatus;
  providerReference: string | null;
  createdAt: string;
}