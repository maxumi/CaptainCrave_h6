import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslocoModule } from '@jsverse/transloco';

import { MenuEditMode } from '../edit-menu.models';

/**
 * Menu editor form component used to create or edit menu items.
 */
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
  @Input() isUploading = false;
  @Input() imageUrl: string | null = null;
  @Output() deleteItem = new EventEmitter<void>();
  @Output() imageSelected = new EventEmitter<File>();

  selectedFile: File | null = null;

  onDeleteItem(): void {
    this.deleteItem.emit();
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  uploadImage(): void {
    if (!this.selectedFile) {
      return;
    }

    this.imageSelected.emit(this.selectedFile);
  }
}