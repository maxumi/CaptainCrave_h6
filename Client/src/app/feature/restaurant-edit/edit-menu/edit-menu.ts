import { Component, OnInit, inject, signal, input } from '@angular/core';
import { form, min } from '@angular/forms/signals';
import { MatDialog } from '@angular/material/dialog';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import {
  catchError,
  firstValueFrom,
  map,
  of,
  switchMap,
  tap,
} from 'rxjs';

import { DeleteDialog } from './delete-dialog/delete-dialog';
import { SaveDialog } from './save-dialog/save-dialog';
import { MenuEditStore } from './menu-edit-store';
import { MenuItem } from './edit-menu.models';
import { MenuItemList } from './menu-item-list/menu-item-list';
import { MenuEditorForm } from './menu-editor-form/menu-editor-form';
import { MenuItemApiService } from '../../../shared/menu-item-api.service';

@Component({
  selector: 'app-edit-menu',
  imports: [TranslocoModule, MenuItemList, MenuEditorForm],
  templateUrl: './edit-menu.html',
  styleUrl: './edit-menu.css',

  providers: [MenuEditStore],
})
export class EditMenu implements OnInit {
  private static readonly DIALOG_CONFIG = {
    width: '250px',
    enterAnimationDuration: '200ms',
    exitAnimationDuration: '150ms',
  };

  private readonly dialog = inject(MatDialog);
  private readonly transloco = inject(TranslocoService);
  private readonly menuItemApiService = inject(MenuItemApiService);

  readonly restaurantId = input<number | null>(null);

  readonly store = inject(MenuEditStore);

  readonly menuModel = signal<MenuItem>(this.store.createDraftItem());
  readonly currency = 'kr.';

  readonly menuForm = form(
    this.menuModel,
    (path) => {
      min(path.price, 1, {
        message: 'Price must be at least 1',
      });
    },
    {
      submission: {
        action: () => this.submitMenuForm(),
      },
    }
  );

  ngOnInit(): void {
    this.store.load(this.restaurantId());
  }

  selectMenu(menuId: number): void {
    this.store.selectMenu(menuId);
    this.store.selectedItem.set(null);
    this.store.mode.set('edit');
    this.menuModel.set(this.store.createDraftItem({
      menuId,
      categoryId: null,
    }));
  }

  resetItemDraft(): void {
    this.store.selectedItem.set(null);
    this.store.mode.set('edit');
    this.store.errorMessage.set('');
    this.menuModel.set(this.store.createDraftItem({
      menuId: this.store.menuId() ?? 0,
      categoryId: null,
    }));
  }

  selectItem(item: MenuItem): void {
    const draft = this.store.selectItem(item);

    if (draft) {
      this.menuModel.set(draft);
      return;
    }

    this.menuModel.set(this.store.createDraftItem({
      menuId: this.store.menuId() ?? 0,
      categoryId: null,
    }));
  }

  startCreateItem(): void {
    const draft = this.store.startCreateItem();

    if (draft) {
      this.menuModel.set(draft);
    }
  }

  createMenu(): void {
    const menuName = window.prompt(this.transloco.translate('menuEdit.createMenuPrompt'));

    if (menuName == null) {
      return;
    }

    const cleanName = menuName.trim();

    if (!cleanName) {
      this.store.errorMessage.set(this.transloco.translate('menuEdit.error.invalidMenuName'));
      return;
    }

    this.store.createMenu(cleanName).subscribe({
      next: (menu) => {
        this.store.selectMenu(menu.id);
        this.menuModel.set(this.store.createDraftItem({
          menuId: menu.id,
          categoryId: null,
        }));
      },
    });
  }

  private submitMenuForm() {
    return firstValueFrom(
      this.dialog.open(SaveDialog, EditMenu.DIALOG_CONFIG).afterClosed().pipe(
        switchMap((confirmed) => {
          if (!confirmed) {
            return of(null);
          }

          return this.store.saveItem(this.menuModel()).pipe(
            tap((saved) => {
              this.menuModel.set(saved);
            }),
            map(() => null),
          );
        }),
        catchError(() => of({
          kind: 'serverError' as const,
          message: this.store.errorMessage(),
        })),
      ),
    );
  }

  uploadMenuImage(file: File): void {
    const itemId = this.menuModel().id;

    if (!itemId || itemId <= 0) {
      this.store.errorMessage.set(this.transloco.translate('menuEdit.error.saveItemFirst'));
      return;
    }

    this.menuItemApiService.uploadImage(itemId, file).pipe(
      tap((updated) => {
        const nextItem = { ...this.menuModel(), imageUrl: updated.imageUrl };
        this.menuModel.set(nextItem);
        this.store.selectedItem.update((current) =>
          current && current.id === updated.id ? { ...current, imageUrl: updated.imageUrl } : current
        );
        this.store.allMenuItems.update((items) =>
          items.map((item) => item.id === updated.id ? { ...item, imageUrl: updated.imageUrl } : item)
        );
        this.store.menuItems.update((items) =>
          items.map((item) => item.id === updated.id ? { ...item, imageUrl: updated.imageUrl } : item)
        );
      }),
      catchError(() => {
        this.store.errorMessage.set(this.transloco.translate('menuEdit.error.uploadFailed'));
        return of(null);
      }),
    ).subscribe();
  }

  confirmDelete(): void {
    this.dialog.open(DeleteDialog, EditMenu.DIALOG_CONFIG).afterClosed().pipe(
      switchMap((confirmed) => {
        if (!confirmed) {
          return of(false);
        }
        return this.store.deleteSelected();
      }),
      tap((deleted) => {
        if (deleted) {
          this.menuModel.set(this.store.createDraftItem({
            menuId: this.store.menuId() ?? 0,
            categoryId: null,
          }));
        }
      }),
    ).subscribe();
  }

  deleteSelectedMenu(): void {
    const menuId = this.store.menuId();
    if (menuId == null) {
      return;
    }

    this.dialog.open(DeleteDialog, EditMenu.DIALOG_CONFIG).afterClosed().pipe(
      switchMap((confirmed) => {
        if (!confirmed) {
          return of(false);
        }

        return this.store.deleteMenu(menuId);
      }),
      tap((deleted) => {
        if (deleted) {
          this.store.errorMessage.set('');
          this.menuModel.set(this.store.createDraftItem({
            menuId: this.store.menuId() ?? 0,
            categoryId: null,
          }));
        }
      }),
    ).subscribe();
  }
}