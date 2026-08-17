import { Component, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { MenuItemDto } from '../../../shared/menu-item-api.service';

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
