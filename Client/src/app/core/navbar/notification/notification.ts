import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../../auth/auth.service';

export interface AppNotification {
  type: 'newOrder' | 'orderStatusChanged';
  message: string;
  orderId: number;
  status?: string;
}

interface NewOrderPayload {
  orderId: number;
}

interface OrderStatusChangedPayload {
  orderId: number;
  status: string;
}

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './notification.html',
  styleUrls: ['./notification.css'],
})
export class Notification implements OnInit, OnDestroy {
  notifications: AppNotification[] = [];
  private connection?: signalR.HubConnection;
  isOpen = false;
  private readonly authService = inject(AuthService);

  notificationsHubUrl = "/hubs/notifications";

  get unreadCount(): number {
    return this.notifications.length;
  }

  toggleNotifications(): void {
    this.isOpen = !this.isOpen;
  }

  clearNotifications(): void {
    this.notifications = [];
  }

  ngOnInit(): void {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.notificationsHubUrl, {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('NewOrder', (payload: NewOrderPayload) => {
      this.notifications.unshift({
        type: 'newOrder',
        message: `New order #${payload.orderId}`,
        orderId: payload.orderId
      });
    });

    this.connection.on(
      'OrderStatusChanged',
      (payload: OrderStatusChangedPayload) => {
        this.notifications.unshift({
          type: 'orderStatusChanged',
          message: `Order #${payload.orderId} is now ${payload.status}`,
          orderId: payload.orderId,
          status: payload.status
        });
      }
    );

    this.connection.start()
      .then(() => console.log('SignalR connected'))
      .catch(error =>
        console.error('SignalR connection failed:', error)
      );
  }

  ngOnDestroy(): void {
    void this.connection?.stop();
  }
}