import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { MenuItem } from '../edit-menu.models';

@Component({
  selector: 'app-menu-item-list',
  imports: [TranslocoModule],
  templateUrl: './menu-item-list.html',
  styleUrl: './menu-item-list.css',
})
export class MenuItemList {
  @Input() menuName: string | null = null;
  @Input() menuItems: MenuItem[] = [];
  @Input() selectedItemId: number | null = null;
  @Input() isMenuSelected = true;
  @Input() currency = '';

  @Output() addItem = new EventEmitter<void>();
  @Output() deleteMenu = new EventEmitter<void>();
  @Output() selectItem = new EventEmitter<MenuItem>();

  onAddItem(): void {
    this.addItem.emit();
  }

  onDeleteMenu(): void {
    this.deleteMenu.emit();
  }

  onSelectItem(item: MenuItem): void {
    this.selectItem.emit(item);
  }

}
