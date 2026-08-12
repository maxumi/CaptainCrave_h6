import { AfterViewInit, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import * as L from 'leaflet';
import { LocationResult, LocationService } from '../../shared/LocationService';
import { AuthService } from '../../core/auth/auth.service';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';
import { UserApiService } from '../../shared/user-api.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, TranslocoModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit, AfterViewInit {
  private readonly locationService = inject(LocationService);
  private readonly userApiService = inject(UserApiService);
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly authService = inject(AuthService);
  private readonly transloco = inject(TranslocoService);

  readonly name = signal('');
  readonly address = signal('');
  readonly addressInput = signal('');
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal<string | null>(null);

  private map!: L.Map;
  private marker!: L.Marker;
  private restaurantMarkers: L.Marker[] = [];

  private readonly defaultLocation: LocationResult;

  constructor() {
    this.defaultLocation = this.locationService.defaultLocation;

    const user = this.authService.user();
    if (user) {
      this.name.set(user.name);
      this.address.set(user.address || this.defaultLocation.label);
      this.addressInput.set(user.address || this.defaultLocation.label);
    } else {
      this.name.set('');
      this.address.set(this.defaultLocation.label);
      this.addressInput.set(this.defaultLocation.label);
    }
  }

  ngOnInit(): void {
    this.userApiService.getMyProfile().subscribe({
      next: (profile) => {
        this.name.set(profile.name);
        this.address.set(profile.address || this.defaultLocation.label);
        this.addressInput.set(profile.address || this.defaultLocation.label);

        this.authService.updateCurrentUserProfile({
          name: profile.name,
          address: profile.address || this.defaultLocation.label,
          latitude: profile.latitude,
          longitude: profile.longitude,
        });

        if (this.map) {
          this.updateLocation(
            {
              lat: profile.latitude ?? this.defaultLocation.lat,
              lng: profile.longitude ?? this.defaultLocation.lng,
              label: profile.address || this.defaultLocation.label,
            },
            profile.address || this.defaultLocation.label
          );
        }
      },
      error: (error) => {
        console.error('Error loading profile:', error);
      },
    });
  }

  ngAfterViewInit(): void {
    const user = this.authService.user();
    const latitude = user?.latitude ?? this.defaultLocation.lat;
    const longitude = user?.longitude ?? this.defaultLocation.lng;
    const location: L.LatLngExpression = [latitude, longitude];

    this.map = L.map('profile-map').setView(location, 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(this.map);

    this.marker = L.marker(location)
      .addTo(this.map)
      .bindPopup(this.address() || this.defaultLocation.label);

    this.loadRestaurants(latitude, longitude);

    setTimeout(() => {
      this.map.invalidateSize();
    }, 0);
  }

  async saveAddress(): Promise<void> {
    const trimmedAddress = this.addressInput().trim();
    this.saveError.set(null);
    this.saveSuccess.set(null);

    if (!trimmedAddress) {
      this.saveError.set(this.t('profile.error.addressRequired'));
      return;
    }

    try {
      const location = await firstValueFrom(
        this.locationService.geocodeAddress(trimmedAddress)
      );

      if (!location) {
        this.saveError.set(this.t('profile.error.addressNotFound'));
        return;
      }

      const updated = await firstValueFrom(
        this.userApiService.updateMyProfile({
          name: this.name(),
          address: location.label,
          latitude: location.lat,
          longitude: location.lng,
        })
      );

      this.address.set(updated.address || location.label);
      this.addressInput.set(updated.address || location.label);

      this.authService.updateCurrentUserProfile({
        name: updated.name,
        address: updated.address || location.label,
        latitude: updated.latitude,
        longitude: updated.longitude,
      });

      this.updateLocation(
        {
          lat: updated.latitude ?? location.lat,
          lng: updated.longitude ?? location.lng,
          label: updated.address || location.label,
        },
        updated.address || location.label
      );

      this.saveSuccess.set(this.t('profile.success.addressUpdated'));
    } catch (error) {
      console.error('Error saving profile address:', error);
      this.saveError.set(this.t('profile.error.saveFailed'));
    }
  }

  private updateLocation(location: LocationResult, popupText: string): void {
    const latLng: L.LatLngExpression = [location.lat, location.lng];

    this.marker.setLatLng(latLng);
    this.marker.bindPopup(popupText).openPopup();

    this.map.setView(latLng, 15);

    this.loadRestaurants(location.lat, location.lng);
  }

  private loadRestaurants(lat: number, lng: number): void {
    this.clearRestaurantMarkers();

    this.restaurantApiService.getNearbyRestaurants(lat, lng).subscribe({
      next: (restaurants) => {
        restaurants.forEach((restaurant) => {
          const restaurantPopupText = this.getRestaurantPopupText(restaurant);

          const restaurantMarker = L.marker([
            restaurant.latitude,
            restaurant.longitude,
          ])
            .addTo(this.map)
            .bindPopup(restaurantPopupText);

          this.restaurantMarkers.push(restaurantMarker);
        });
      },
      error: (error) => {
        console.error('Error loading nearby restaurants:', error);
      },
    });
  }

  private getRestaurantPopupText(restaurant: RestaurantDto): string {
    return `<strong>${restaurant.name}</strong><br>${restaurant.address}`;
  }

  private clearRestaurantMarkers(): void {
    this.restaurantMarkers.forEach((marker) => {
      marker.remove();
    });

    this.restaurantMarkers = [];
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}