import { AfterViewInit, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs';
import * as L from 'leaflet';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { LocationService } from '../../shared/LocationService';

const MAX_DISTANCE_KM = 50;

@Component({
  selector: 'app-nearby-restaurants-map',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './nearby-restaurants-map.html',
  styleUrl: './nearby-restaurants-map.css',
})
export class NearbyRestaurantsMap implements OnInit, AfterViewInit, OnDestroy {
  private readonly restaurantApiService = inject(RestaurantApiService);
  private readonly authService = inject(AuthService);
  private readonly locationService = inject(LocationService);
  private readonly transloco = inject(TranslocoService);

  readonly selectedLocation = this.locationService.selectedLocation;
  readonly addressInput = signal(this.selectedLocation().label);
  readonly nearbyRestaurants = signal<RestaurantDto[]>([]);
  readonly isLoading = signal(true);
  readonly isResolvingAddress = signal(false);
  readonly loadError = signal<string | null>(null);

  private map!: L.Map;
  private centerMarker!: L.Marker;
  private restaurantMarkers: L.Marker[] = [];

  ngOnInit(): void {
    const user = this.authService.user();

    if (
      user?.latitude != null &&
      user?.longitude != null &&
      !this.locationService.hasUserSelectedLocation()
    ) {
      this.locationService.setSelectedLocationByValues(
        user.latitude,
        user.longitude,
        user.address || this.selectedLocation().label,
        'profile'
      );
      this.addressInput.set(this.locationService.selectedLocation().label);
    }
  }

  ngAfterViewInit(): void {
    const currentLocation = this.selectedLocation();
    const mapCenter: L.LatLngExpression = [
      currentLocation.lat,
      currentLocation.lng,
    ];

    this.map = L.map('nearby-restaurants-map').setView(mapCenter, 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(this.map);

    this.centerMarker = L.marker(mapCenter)
      .addTo(this.map)
      .bindPopup(currentLocation.label);

    this.loadNearbyRestaurantsFromSelectedLocation();

    setTimeout(() => this.map.invalidateSize(), 0);
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  setLocationFromAddress(): void {
    const address = this.addressInput().trim();

    if (!address) {
      this.loadError.set(this.t('restaurants.error.locationRequired'));
      return;
    }

    this.loadError.set(null);
    this.isResolvingAddress.set(true);

    this.locationService
      .geocodeAddress(address)
      .pipe(finalize(() => this.isResolvingAddress.set(false)))
      .subscribe({
        next: (location) => {
          if (!location) {
            this.loadError.set(this.t('restaurants.error.locationNotFound'));
            return;
          }

          this.locationService.setSelectedLocation(location, 'guest');
          this.addressInput.set(location.label);
          this.loadNearbyRestaurantsFromSelectedLocation();
        },
        error: () => {
          this.loadError.set(this.t('restaurants.error.loadFailed'));
        },
      });
  }

  useDefaultLocation(): void {
    this.locationService.resetSelectedLocation();
    this.addressInput.set(this.locationService.selectedLocation().label);
    this.loadError.set(null);
    this.loadNearbyRestaurantsFromSelectedLocation();
  }

  private loadNearbyRestaurantsFromSelectedLocation(): void {
    const location = this.selectedLocation();

    this.updateCenterMarker(location.lat, location.lng, location.label);
    this.clearRestaurantMarkers();
    this.isLoading.set(true);

    this.restaurantApiService
      .getNearbyRestaurants(location.lat, location.lng, MAX_DISTANCE_KM)
      .subscribe({
        next: (restaurants) => {
          this.nearbyRestaurants.set(restaurants);
          restaurants.forEach((restaurant) => this.addRestaurantMarker(restaurant));
          this.isLoading.set(false);
        },
        error: () => {
          this.loadError.set(this.t('restaurants.error.loadFailed'));
          this.isLoading.set(false);
        },
      });
  }

  private addRestaurantMarker(restaurant: RestaurantDto): void {
    const marker = L.marker([restaurant.latitude, restaurant.longitude])
      .addTo(this.map)
      .bindPopup(`<strong>${restaurant.name}</strong><br>${restaurant.address}`);

    this.restaurantMarkers.push(marker);
  }

  private updateCenterMarker(lat: number, lng: number, label: string): void {
    const latLng: L.LatLngExpression = [lat, lng];

    this.centerMarker.setLatLng(latLng);
    this.centerMarker.bindPopup(label).openPopup();
    this.map.setView(latLng, 13);
  }

  private clearRestaurantMarkers(): void {
    this.restaurantMarkers.forEach((marker) => marker.remove());
    this.restaurantMarkers = [];
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}