import { Injectable, inject, signal } from '@angular/core';
import { EMPTY, Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, switchMap } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { MenuItem, MenuEditMode, UpdateMenuItemRequest } from './edit-menu.models';
import { MenuItemApiService, MenuItemDto } from '../../../shared/menu-item-api.service';
import { RestaurantApiService } from '../../../shared/restaurant-api.service';

/**
 * Store used by MenuEditComponent to manage state and logic
 * related to editing menu items.
 *
 * It can also be reused by future components that need the same
 * menu editing behavior.
 */
@Injectable()
export class MenuEditStore {
  private readonly menuApiService = inject(MenuItemApiService);
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly translocoService = inject(TranslocoService);

  /**mode menas either "edit" or "create" */
  readonly mode = signal<MenuEditMode>('edit');
  readonly isSubmitting = signal(false);
  readonly isLoading = signal(true);
  readonly hasNoRestaurant = signal(false);
  readonly errorMessage = signal('');

  readonly menuItems = signal<MenuItem[]>([]);

  /** Selected item in use */
  readonly selectedItem = signal<MenuItem | null>(null);

  /** The restaurantId is needed to know which restaurant owns the active menu. */
  readonly restaurantId = signal<number | null>(null);
  readonly menuId = signal<number | null>(null);

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.hasNoRestaurant.set(false);
    this.clearRestaurantState();

    this.restaurantApiService.getMyRestaurant().pipe(
      switchMap(restaurant => {
        this.restaurantId.set(restaurant.id);

        return this.menuApiService.getMenuItemsByRestaurant(restaurant.id);
      }),

      map((items: MenuItemDto[]) => items.map(item => this.toLocalItem(item))),

      catchError((error: { status?: number }) => {
        if (error.status === 404) {
          this.hasNoRestaurant.set(true);
          this.clearRestaurantState();
        } else {
          this.errorMessage.set(this.t('error.loadFailed'));
        }
        // empty observable.
        return EMPTY;
      }),

      finalize(() => {
        this.isLoading.set(false);
      }),
    ).subscribe(items => {
      this.menuItems.set(items);

      if (!this.menuId() && items.length > 0) {
        this.menuId.set(items[0].menuId);
      }
    });
  }

selectItem(item: MenuItem): MenuItem | null {
  const selected = this.selectedItem();

  // if the same item is selected again, deselect it and switch to edit mode.
  if (selected?.id === item.id) {
    this.selectedItem.set(null);
    this.mode.set('edit');
    this.errorMessage.set('');

    return null;
  }

  this.selectedItem.set(item);
  this.mode.set('edit');
  this.errorMessage.set('');

  return structuredClone(item);
}

  // A menu item can only be created when a restaurant has been loaded.
  startCreateItem(): MenuItem | null {
    const restaurantId = this.restaurantId();
    const menuId = this.menuId();

    if (restaurantId == null) {
      this.errorMessage.set(this.t('error.missingRestaurant'));
      return null;
    }

    if (menuId == null || menuId <= 0) {
      this.errorMessage.set(this.t('error.missingMenu'));
      return null;
    }

    this.errorMessage.set('');
    this.selectedItem.set(null);
    this.mode.set('create');

    return this.createDraftItem({
      menuId,
      categoryId: null,
    });
  }

  saveItem(item: MenuItem): Observable<MenuItem> {
    const restaurantId = this.restaurantId();
    const menuId = item.menuId || this.menuId();

    if (restaurantId == null) {
      const message = this.t('error.missingRestaurant');
      this.errorMessage.set(message);
      return throwError(() => new Error(message));
    }

    if (menuId == null || menuId <= 0) {
      const message = this.t('error.missingMenu');
      this.errorMessage.set(message);
      return throwError(() => new Error(message));
    }

    this.errorMessage.set('');
    this.isSubmitting.set(true);

    // New items are created. Existing items are updated.
    const isNewItem = this.mode() === 'create' || item.id === 0;
    const payload = this.toPayload(item, menuId);

    const request = isNewItem
      ? this.menuApiService.create(payload)
      : this.menuApiService.update(item.id, payload);


    return request.pipe(
      map(saved => this.updateStateAfterSave(saved, isNewItem)),

      catchError(error => {
        this.errorMessage.set(this.t('error.saveFailed'));
        return throwError(() => error);
      }),

      finalize(() => {
        this.isSubmitting.set(false);
      }),
    );
  }

  deleteSelected(): Observable<boolean> {
    const selected = this.selectedItem();

    if (!selected) {
      return of(false);
    }

    this.errorMessage.set('');

    return this.menuApiService.delete(selected.id).pipe(
      map(() => {
        this.removeMenuItem(selected.id);
        this.selectedItem.set(null);
        this.mode.set('edit');

        return true;
      }),

      catchError(() => {
        this.errorMessage.set(this.t('error.deleteFailed'));
        return of(false);
      }),
    );
  }

  private t(key: string): string {
    return this.translocoService.translate(`menuEdit.${key}`);
  }

  /**
   * Creates a draft menu item used when starting item creation.
   *
   * The draft is only used locally in the form and is not saved until saveItem is called.
   *
   */
  createDraftItem(overrides: Partial<MenuItem> = {}): MenuItem {
    return {
      id: 0,
      menuId: this.menuId() ?? 0,
      categoryId: null,
      name: '',
      description: '',
      imageUrl: '',
      price: 1,
      isAvailable: true,
      ...overrides,
    };
  }
  
  /**
   * Updates local store state after a menu item has been saved.
   *
   * New items are added to the list, while existing items replace
   * their previous version. The saved item is then selected and the form returns to edit mode.
   */
  private updateStateAfterSave(
    savedItem: MenuItem,
    isNewItem: boolean
  ): MenuItem {
    if (isNewItem) {
      this.addMenuItem(savedItem);
    } else {
      this.updateMenuItem(savedItem);
    }

    this.selectedItem.set(savedItem);
    this.mode.set('edit');

    return structuredClone(savedItem);
  }

  private addMenuItem(item: MenuItem): void {
    this.menuItems.set([item, ...this.menuItems()]);
  }

  private updateMenuItem(item: MenuItem): void {
    this.menuItems.set(
      this.menuItems().map(existingItem =>
        existingItem.id === item.id ? item : existingItem
      )
    );
  }

  private removeMenuItem(itemId: number): void {
    this.menuItems.set(
      this.menuItems().filter(item => item.id !== itemId)
    );
  }

  private clearRestaurantState(): void {
    this.menuItems.set([]);
    this.selectedItem.set(null);
    this.restaurantId.set(null);
    this.menuId.set(null);
  }

  private toLocalItem(dto: MenuItemDto): MenuItem {
    return {
      id: dto.id,
      menuId: dto.menuId,
      categoryId: dto.categoryId,
      name: dto.name,
      description: dto.description,
      imageUrl: dto.imageUrl,
      price: dto.price,
      isAvailable: dto.isAvailable,
    };
  }

  private toPayload(
    item: MenuItem,
    menuId: number
  ): UpdateMenuItemRequest {
    return {
      menuId,
      categoryId: item.categoryId,
      name: item.name,
      description: item.description,
      price: item.price,
      imageUrl: item.imageUrl,
      isAvailable: item.isAvailable,
    };
  }
}