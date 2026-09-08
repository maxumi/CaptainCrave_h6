// Order status enum representing the different stages of an order.
export enum OrderStatus {
  Pending = 'pending',
  Preparing = 'preparing',
  OnTheWay = 'onTheWay',
  ReadyForPickup = 'readyForPickup',
  Delivered = 'delivered',
  Cancelled = 'cancelled',
  // Payment-related status for orders awaiting payment
  AwaitingPayment = 'awaitingPayment'
}
// Delivery type enum representing the different methods of order fulfillment.
export enum DeliveryType{
  Delivery = "delivery",
  Pickup = "pickup",
}