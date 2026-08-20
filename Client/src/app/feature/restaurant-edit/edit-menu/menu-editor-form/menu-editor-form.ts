import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslocoModule } from '@jsverse/transloco';

import { MenuEditMode } from '../edit-menu.models';

@Component({
  selector: 'app-menu-editor-form',
  imports: [CommonModule, TranslocoModule, FormField, FormRoot],
  templateUrl: './menu-editor-form.html',
  styleUrl: './menu-editor-form.css',
})
export class MenuEditorForm {
  @Input() mode: MenuEditMode = 'edit';
  @Input() itemName = '';
  @Input() isSubmitting = false;
  @Input() menuForm: any;
  @Input() canUpload = false;

  @Output() deleteItem = new EventEmitter<void>();
  @Output() imageSelected = new EventEmitter<File>();

  selectedFile: File | null = null;

  onDeleteItem(): void {
    this.deleteItem.emit();
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      return;
    }

    this.selectedFile = file;
    this.imageSelected.emit(file);
  }
}
