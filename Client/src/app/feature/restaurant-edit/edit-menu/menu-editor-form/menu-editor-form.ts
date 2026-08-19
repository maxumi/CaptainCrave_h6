import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslocoModule } from '@jsverse/transloco';

import { MenuEditMode } from '../edit-menu.models';

@Component({
  selector: 'app-menu-editor-form',
  imports: [TranslocoModule, FormField, FormRoot],
  templateUrl: './menu-editor-form.html',
  styleUrl: './menu-editor-form.css',
})
export class MenuEditorForm {
  @Input() mode: MenuEditMode = 'edit';
  @Input() itemName = '';
  @Input() isSubmitting = false;
  @Input() menuForm: any;

  @Output() deleteItem = new EventEmitter<void>();

  onDeleteItem(): void {
    this.deleteItem.emit();
  }

}
