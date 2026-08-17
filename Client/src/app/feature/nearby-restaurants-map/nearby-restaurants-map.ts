import { AfterViewInit, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs';
import * as L from 'leaflet';
import { RestaurantApiService, RestaurantDto } from '../../shared/restaurant-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { LocationService } from '../../shared/LocationService';
import { FormsModule } from '@angular/forms';

const MAX_DISTANCE_KM = 50;

@Component({
  selector: 'app-nearby-restaurants-map',
  imports: [TranslocoModule, FormsModule],
  templateUrl: './nearby-restaurants-map.html',
  styleUrl: './nearby-restaurants-map.css',
})
export class NearbyRestaurantsMap implements OnInit, AfterViewInit, OnDestroy {
    private readonly restaurantApiService = inject(RestaurantApiService);
    private readonly authService = inject(AuthService);
    private readonly locationService = inject(LocationService);
    private readonly transloco = inject(TranslocoService);

    // Signals for state
    readonly selectedLocation = this.locationService.selectedLocation;
    readonly addressInput = signal(this.selectedLocation().label);
    readonly nearbyRestaurants = signal<RestaurantDto[]>([]);
    readonly isLoading = signal(true);
    readonly isResolvingAddress = signal(false);
    readonly loadError = signal<string | null>(null);

    // Leaflet map
    private map!: L.Map;

    // Marker for the selected location(center of the map)
    private centerMarker!: L.Marker;

    // Keeps all restaurant markers in one layer so they can be cleared together.
    private readonly restaurantMarkers = L.layerGroup();

  ngOnInit(): void {
    const user = this.authService.user();

    // Use the saved profile location unless the user has already selected another location.
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

    // Add the shared restaurant marker layer to the map once.
    this.restaurantMarkers.addTo(this.map);

    this.centerMarker = L.marker(mapCenter)
      .addTo(this.map)
      .bindPopup(currentLocation.label);

    this.loadNearbyRestaurantsFromSelectedLocation();

    // Ensures Leaflet recalculates the map size after Angular finishes rendering.
    setTimeout(() => this.map.invalidateSize(), 0);
  }

  ngOnDestroy(): void {
    // Clean up Leaflet resources when leaving the component.
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
      // Always reset the resolving state, whether the request succeeds or fails.
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
      // Keeps loading-state cleanup in one place for success and error cases.
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (restaurants) => {
          this.nearbyRestaurants.set(restaurants);

          restaurants.forEach((restaurant) =>
            this.addRestaurantMarker(restaurant)
          );
        },
        error: () => {
          this.loadError.set(this.t('restaurants.error.loadFailed'));
        },
      });
  }

  private addRestaurantMarker(restaurant: RestaurantDto): void {
    L.marker([restaurant.latitude, restaurant.longitude])
      .bindPopup(
        `<strong>${restaurant.name}</strong><br>${restaurant.address}`
      )
      .addTo(this.restaurantMarkers);
  }

  private updateCenterMarker(
    lat: number,
    lng: number,
    label: string
  ): void {
    const latLng: L.LatLngExpression = [lat, lng];

    this.centerMarker.setLatLng(latLng);
    this.centerMarker.bindPopup(label).openPopup();
    this.map.setView(latLng, 13);
  }

  private clearRestaurantMarkers(): void {
    // Clears all restaurant markers without removing the layer itself.
    this.restaurantMarkers.clearLayers();
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}