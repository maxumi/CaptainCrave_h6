import { AfterViewInit, Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { catchError, firstValueFrom, map, of } from 'rxjs';
import * as L from 'leaflet';

import {
  CreateRestaurantRequest,
  RestaurantApiService,
} from '../../shared/restaurant-api.service';
import { LocationResult, LocationService } from '../../shared/LocationService';
import { AuthService } from '../../core/auth/auth.service';
import { configureLeafletDefaultIcons } from '../../shared/leaflet-icons';

interface RestaurantFormData {
  restaurantName: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
}

@Component({
  selector: 'app-restaurant-create',
  imports: [FormRoot, FormField, TranslocoModule],
  templateUrl: './restaurant-create.html',
  styleUrl: './restaurant-create.css',
})
export class RestaurantCreate implements OnInit, AfterViewInit {
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly authService = inject(AuthService);
  private readonly locationService = inject(LocationService);
  private readonly translocoService = inject(TranslocoService);
  private readonly router = inject(Router);

  readonly user = this.authService.user;
  readonly isSubmitting = signal(false);
  readonly submitError = signal<string | null>(null);

  private map!: L.Map;
  private marker!: L.Marker;

  readonly restaurantModel = signal<RestaurantFormData>({
    restaurantName: '',
    description: '',
    address: '',
    latitude: 0,
    longitude: 0,
  });

  readonly restaurantForm = form(
    this.restaurantModel,
    (schemaPath) => {
      required(schemaPath.restaurantName, {
        message: this.t('restaurantCreate.validation.restaurantNameRequired'),
      });

      required(schemaPath.address, {
        message: this.t('restaurantCreate.validation.addressRequired'),
      });

      required(schemaPath.latitude, {
        message: this.t('restaurantCreate.validation.latitudeRequired'),
      });

      required(schemaPath.longitude, {
        message: this.t('restaurantCreate.validation.longitudeRequired'),
      });
    },
    {
      submission: {
        action: () => this.submitRestaurantForm(),
      },
    }
  );

  ngOnInit(): void {
    this.redirectIfRestaurantExists();
  }

  ngAfterViewInit(): void {
    configureLeafletDefaultIcons();

    const defaultLocation = this.locationService.defaultLocation;
    const location: L.LatLngExpression = [
      defaultLocation.lat,
      defaultLocation.lng,
    ];

    this.map = L.map('restaurant-create-map').setView(location, 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(this.map);

    this.marker = L.marker(location)
      .addTo(this.map)
      .bindPopup(defaultLocation.label);

    setTimeout(() => this.map.invalidateSize(), 0);
  }

  setLocationFromAddress(): void {
    const address = this.restaurantModel().address.trim();

    if (!address) {
      return;
    }

    this.locationService.geocodeAddress(address).subscribe({
      next: (location) => {
        if (!location) {
          return;
        }

        this.restaurantModel.update((model) => ({
          ...model,
          address: location.label,
          latitude: location.lat,
          longitude: location.lng,
        }));

        this.updateMapLocation(location);
      },
      error: () => {
        this.submitError.set(this.t('restaurantCreate.validation.addressLookupError'));
      },
    });
  }

  private submitRestaurantForm() {
    const user = this.user();

    if (!user) {
      return this.failSubmit('restaurantCreate.validation.userRequired');
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    return firstValueFrom(
      this.restaurantApiService
        .createRestaurant(this.toCreateRestaurantRequest(user.userId))
        .pipe(
          map(() => {
            this.finishSubmitting();
            void this.router.navigateByUrl('/');
            return null;
          }),
          catchError((error: HttpErrorResponse) => {
            this.finishSubmitting();

            if (this.isRestaurantAlreadyExistsError(error)) {
              void this.router.navigateByUrl('/');
              return of(null);
            }

            const message = this.t('restaurantCreate.validation.serverError');
            this.submitError.set(message);

            return of({
              kind: 'serverError' as const,
              message,
            });
          })
        )
    );
  }

  private toCreateRestaurantRequest(userId: number): CreateRestaurantRequest {
    const model = this.restaurantModel();

    return {
      userId,
      name: model.restaurantName,
      description: model.description,
      address: model.address,
      latitude: model.latitude,
      longitude: model.longitude,
      imageUrl: '',
      isActive: true,
    };
  }

  private redirectIfRestaurantExists(): void {
    this.restaurantApiService
      .getMyRestaurant()
      .pipe(
        map(() => true),
        catchError(() => of(false))
      )
      .subscribe((hasRestaurant) => {
        if (hasRestaurant) {
          void this.router.navigateByUrl('/');
        }
      });
  }

  private failSubmit(key: string) {
    const message = this.t(key);
    this.submitError.set(message);

    return Promise.resolve({
      kind: 'serverError' as const,
      message,
    });
  }

  private finishSubmitting(): void {
    this.isSubmitting.set(false);
  }

  private isRestaurantAlreadyExistsError(error: HttpErrorResponse): boolean {
    const message = this.getServerErrorMessage(error);

    return (
      error.status === 409 ||
      message.includes('already exists') ||
      message.includes('already exist')
    );
  }

  private getServerErrorMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string') {
      return error.error.toLowerCase();
    }

    if (typeof error.error?.message === 'string') {
      return error.error.message.toLowerCase();
    }

    return '';
  }

  private updateMapLocation(location: LocationResult): void {
    const latLng: L.LatLngExpression = [location.lat, location.lng];

    this.marker.setLatLng(latLng);
    this.marker.bindPopup(location.label).openPopup();
    this.map.setView(latLng, 15);
  }

  private t(key: string): string {
    return this.translocoService.translate(key);
  }
}