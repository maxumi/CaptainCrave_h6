import { Routes } from '@angular/router';
import { Home } from './feature/home/home';
import { Login } from './feature/login/login';
import { Register } from './feature/register/register';
import { Restaurants } from './feature/restaurants/restaurants';
import { RestaurantInfo } from './feature/restaurant-info/restaurant-info';
import { authGuard, guestGuard } from './core/auth/auth-guard';
import { Profile } from './feature/profile/profile';
import { Cart } from './feature/cart/cart';
import { RestaurantEdit } from './feature/restaurant-edit/restaurant-edit';
import { NotFound } from './core/not-found/not-found';
import { roleGuard } from './core/auth/role-guard';
import { RestaurantCreate } from './feature/restaurant-create/restaurant-create';
import { OrderStatusView } from './feature/order-status-view/order-status-view';
import { OrderHistory } from './feature/order-history/order-history';
import { NearbyRestaurantsMap } from './feature/nearby-restaurants-map/nearby-restaurants-map';
import { RestaurantsSearch } from './feature/restaurants-search/restaurants-search';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'profile', component: Profile, canActivate: [authGuard] },
  { path: 'cart', component: Cart, canActivate: [authGuard, roleGuard], data: { roles: ['Customer'] } },
  { path: 'order-status', component: OrderStatusView, canActivate: [authGuard, roleGuard], data: { roles: ['Customer'] } },
  { path: 'order-history', component: OrderHistory, canActivate: [authGuard, roleGuard], data: { roles: ['Customer'] } },
  { path: "order-status/:id", component: OrderStatusView, canActivate: [authGuard, roleGuard], data: { roles: ['Customer'] } },
  { path: 'restaurant-create', component: RestaurantCreate, canActivate: [authGuard, roleGuard], data: { roles: ['Restaurant'] } },
  { path: 'restaurant-edit', component: RestaurantEdit, canActivate: [authGuard, roleGuard], data: { roles: ['Restaurant', 'Admin'] } },
  { path: 'restaurant-edit/:id', component: RestaurantEdit, canActivate: [authGuard, roleGuard], data: { roles: ['Restaurant', 'Admin'] } },
  { path: 'restaurants', component: Restaurants },
  { path: 'restaurants-search', component: RestaurantsSearch },
  { path: 'restaurants-map', component: NearbyRestaurantsMap },
  { path: 'restaurantInfo', component: RestaurantInfo },
  
  // not found route, should always be last
   { path: '**', component: NotFound }
];