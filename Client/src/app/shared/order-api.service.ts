import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { OrderStatus } from './models/status';


export enum DeliveryType {
  Delivery = 'delivery',
  Pickup = 'pickup',
}

export interface CreateOrderItemRequest {
  menuItemId: number;
  quantity: number;
}

export interface CreateOrderRequest {
  userId: number;
  restaurantId: number;
  deliveryType: DeliveryType;
  deliveryAddress?: string;
  items: CreateOrderItemRequest[];
}

export interface OrderResponse {
  id: number;
  userId: number;
  restaurantId: number;
  totalPrice: number;
}

export interface OrderItemDto {
  id: number;
  menuItemId: number;
  menuItemName: string;
  quantity: number;
  price: number;
}

export interface OrderDto {
  id: number;
  userId: number;
  userName: string;
  userEmail: string;
  restaurantId: number;
  restaurantName: string;
  deliveryType: DeliveryType;
  totalPrice: number;
  status: OrderStatus;
  createdAt: string;
  items: OrderItemDto[];
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
}

@Injectable({
  providedIn: 'root',
})
export class OrderApiService {
  private readonly http = inject(HttpClient);
  private readonly ordersUrl = `${environment.apiUrl}/Orders`;

  createOrder(payload: CreateOrderRequest): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(this.ordersUrl, payload);
  }
  

  getRestaurantActiveOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.ordersUrl}/restaurant/active`);
  }

  getRestaurantHistoricOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.ordersUrl}/restaurant/history`);
  }

  updateOrderStatus(
    orderId: number,
    payload: UpdateOrderStatusRequest
  ): Observable<OrderDto> {
    return this.http.patch<OrderDto>(
      `${this.ordersUrl}/${orderId}/status`,
      payload
    );
  }
  getOrderById(orderId: number): Observable<OrderDto> {
    return this.http.get<OrderDto>(`${this.ordersUrl}/${orderId}`);
  }

  hasOrderedFromRestaurant(
    restaurantId: number
  ): Observable<{ hasOrdered: boolean }> {
    return this.http.get<{ hasOrdered: boolean }>(
      `${this.ordersUrl}/customer/has-ordered/${restaurantId}`
    );
  }

  getCustomerActiveOrder(): Observable<OrderDto | null> {
    return this.http.get<OrderDto>(`${this.ordersUrl}/customer/active`).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return of(null);
        }
        return throwError(() => error);
      })
    );
  }

  // Backward-compatible alias used by existing components.
  getActiveOrder(): Observable<OrderDto | null> {
    return this.getCustomerActiveOrder();
  }

  getCustomerHistoricOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.ordersUrl}/customer/history`).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return of([]);
        }

        return throwError(() => error);
      })
    );
  }
  
  getRestaurantActiveOrdersByRestaurantId(
    restaurantId: number
  ): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(
      `${this.ordersUrl}/restaurant/${restaurantId}/active`
    );
  }

  getRestaurantHistoricOrdersByRestaurantId(
    restaurantId: number
  ): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(
      `${this.ordersUrl}/restaurant/${restaurantId}/history`
    );
  }

}
