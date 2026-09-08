import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { map, Observable, of } from 'rxjs';

export interface LocationResult {
  lat: number;
  lng: number;
  label: string;
}

// Location source type indicating where the location was selected from.
export type LocationSource = 'default' | 'guest' | 'profile';

// Stored location including its source.
export interface StoredLocation extends LocationResult {
  source: LocationSource;
}

// Result format returned by the Nominatim geocoding API.
interface NominatimResult {
  lat: string;
  lon: string;
  display_name: string;
}

const NOMINATIM_URL = 'https://nominatim.openstreetmap.org/search';

@Injectable({
  providedIn: 'root',
})
export class LocationService {
  private readonly storageKey = 'selectedLocation';
  private readonly http = inject(HttpClient);

  // The default location used when no user-selected location is available.
  readonly defaultLocation: LocationResult = {
    lat: 55.6761,
    lng: 12.5683,
    label: 'Copenhagen, Denmark',
  };

  readonly selectedLocation = signal<StoredLocation>(
    this.readStoredLocation()
  );

  readonly hasUserSelectedLocation = computed(
    () => this.selectedLocation().source !== 'default'
  );

  geocodeAddress(address: string): Observable<LocationResult | null> {
    const trimmedAddress = address.trim();

    if (!trimmedAddress) {
      return of(null);
    }

    return this.http
      .get<NominatimResult[]>(NOMINATIM_URL, {
        params: {
          format: 'json',
          q: trimmedAddress,
          limit: 1,
        },
      })
      .pipe(map((results) => this.toLocationResult(results[0])));
  }

  setSelectedLocation(
    location: LocationResult,
    source: LocationSource = 'guest'
  ): void {
    const next: StoredLocation = {
      lat: location.lat,
      lng: location.lng,
      label: location.label,
      source,
    };

    this.persistLocation(next);
    this.selectedLocation.set(next);
  }

  setSelectedLocationByValues(
    lat: number,
    lng: number,
    label: string,
    source: LocationSource = 'guest'
  ): void {
    this.setSelectedLocation({ lat, lng, label }, source);
  }

  resetSelectedLocation(): void {
    const fallback = this.getDefaultStoredLocation();
    this.persistLocation(fallback);
    this.selectedLocation.set(fallback);
  }

  private toLocationResult(
    result: NominatimResult | undefined
  ): LocationResult | null {
    if (!result) {
      return null;
    }

    return {
      lat: Number(result.lat),
      lng: Number(result.lon),
      label: result.display_name,
    };
  }

  private readStoredLocation(): StoredLocation {
    const fallback = this.getDefaultStoredLocation();
    const raw = localStorage.getItem(this.storageKey);

    if (!raw) {
      return fallback;
    }

    try {
      const parsed = JSON.parse(raw) as Partial<StoredLocation>;
      const normalizedLabel =
        typeof parsed.label === 'string' ? parsed.label.trim() : '';

      const hasCoordinates =
        Number.isFinite(parsed.lat) && Number.isFinite(parsed.lng);
      const hasLabel = !!normalizedLabel;

      if (!hasCoordinates || !hasLabel) {
        return fallback;
      }

      return {
        lat: Number(parsed.lat),
        lng: Number(parsed.lng),
        label: normalizedLabel,
        source: this.isValidLocationSource(parsed.source)
          ? parsed.source
          : 'guest',
      };
    } catch {
      return fallback;
    }
  }

  private getDefaultStoredLocation(): StoredLocation {
    return {
      lat: this.defaultLocation.lat,
      lng: this.defaultLocation.lng,
      label: this.defaultLocation.label,
      source: 'default',
    };
  }

  private persistLocation(location: StoredLocation): void {
    localStorage.setItem(this.storageKey, JSON.stringify(location));
  }

  private isValidLocationSource(
    source: unknown
  ): source is LocationSource {
    return source === 'default' || source === 'guest' || source === 'profile';
  }
}