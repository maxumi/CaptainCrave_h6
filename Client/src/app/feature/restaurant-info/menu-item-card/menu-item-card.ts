import { Component, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { MenuItemDto } from '../../../shared/menu-item-api.service';

/**
 * Component for displaying a menu item card, including its details and the option to add it to the cart.
 */
@Component({
  selector: 'app-menu-item-card',
  imports: [TranslocoModule],
  templateUrl: './menu-item-card.html',
  styleUrl: './menu-item-card.css',
})
export class MenuItemCard {
  readonly item = input.required<MenuItemDto>();
  readonly canAddToCart = input(false);

  readonly add = output<void>();

}
