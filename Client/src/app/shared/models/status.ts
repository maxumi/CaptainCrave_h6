export enum OrderStatus {
  Pending = 'pending',
  Preparing = 'preparing',
  OnTheWay = 'onTheWay',
  ReadyForPickup = 'readyForPickup',
  Delivered = 'delivered',
  Cancelled = 'cancelled',
}

export enum DeliveryType{
  Delivery = "delivery",
  Pickup = "pickup",
}