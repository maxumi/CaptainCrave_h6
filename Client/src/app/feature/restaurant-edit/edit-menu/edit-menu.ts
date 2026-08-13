import { Component, OnInit, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, min } from '@angular/forms/signals';
import { MatDialog } from '@angular/material/dialog';
import { TranslocoModule } from '@jsverse/transloco';
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

@Component({
  selector: 'app-edit-menu',
  imports: [TranslocoModule, FormField, FormRoot],
  templateUrl: './edit-menu.html',
  styleUrl: './edit-menu.css',

  // Provides a store instance for this component.
  // prevents menu edit state from leaking into other pages.
  providers: [MenuEditStore],
})
export class EditMenu implements OnInit {
  // Shared dialog settings for save and delete confirmation dialogs.
  private static readonly DIALOG_CONFIG = {
    width: '250px',
    enterAnimationDuration: '200ms',
    exitAnimationDuration: '150ms',
  };

  private readonly dialog = inject(MatDialog);

  // Store handles menu state, loading, saving, deleting, and errors.
  readonly store = inject(MenuEditStore);

  // Local form draft.
  // This is separate from store.selectedItem so changes in the form do not
  // directly modify the item in the menu list before saving.
  readonly menuModel = signal<MenuItem>(this.store.createDraftItem());


  readonly currency = 'kr.';

readonly menuForm = form(
  this.menuModel,
  (path) => {
    min(path.price, 1, {
      message: 'Price must be at least 0',
    });
  },
  {
    submission: {
      action: () => this.submitMenuForm(),
    },
  }
);

  ngOnInit(): void {
    this.store.load();
  }

  selectItem(item: MenuItem): void {
    const draft = this.store.selectItem(item);

    if (draft) {
      this.menuModel.set(draft);
      return;
    }

    this.menuModel.set(this.store.createDraftItem());
  }

  startCreateItem(): void {
    // Store switches to create mode and returns an empty draft item.
    const draft = this.store.startCreateItem();

    // Check Draft is not null, 
    // because startCreateItem can return null if there is no restaurant.
    if (draft) {
      this.menuModel.set(draft);
    }
  }

  private submitMenuForm() {
    // submit seems to require async so use firstValueFrom to convert the observable to a promise.
    return firstValueFrom(
      // Ask the user to confirm before saving.
      this.dialog.open(SaveDialog, EditMenu.DIALOG_CONFIG).afterClosed().pipe(
        switchMap(confirmed => {
          if (!confirmed) {
            return of(null);
          }

          // Save the current form draft through the store.
          return this.store.saveItem(this.menuModel()).pipe(
            tap(saved => {
              // Update the form with the saved item returned from the backend.
              // This is useful for new items because the backend gives them an ID.
              this.menuModel.set(saved);
            }),

            // The signal form expects null when submission succeeds.
            map(() => null),
          );
        }),

        catchError(() => {
          return of({
            kind: 'serverError' as const,
            message: this.store.errorMessage(),
          });
        }),
      ),
    );
  }

  confirmDelete(): void {
    // Ask the user to confirm before deleting.
    this.dialog.open(DeleteDialog, EditMenu.DIALOG_CONFIG).afterClosed().pipe(
      switchMap(confirmed => {
        if (!confirmed) {
          return of(false);
        }
        return this.store.deleteSelected();
      }),

      tap(deleted => {
        // If delete succeeded, reset the form to an empty draft.
        if (deleted) {
          this.menuModel.set(this.store.createDraftItem());
        }
      }),
    ).subscribe();
  }
}