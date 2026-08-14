import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EditOrder } from './edit-order/edit-order';
import { EditMenu } from './edit-menu/edit-menu';

@Component({
  selector: 'app-restaurant-edit',
  imports: [EditMenu, EditOrder],
  templateUrl: './restaurant-edit.html',
  styleUrl: './restaurant-edit.css'
})
export class RestaurantEdit {
  activeTab: 'orders' | 'menu' = 'menu';

}