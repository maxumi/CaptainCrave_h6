import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, of } from 'rxjs';

export interface LocationResult {
  lat: number;
  lng: number;
  label: string;
}

export interface Restaurant {
  id: number;
  lat: number;
  lng: number;
  name: string;
  cuisine?: string;
}

interface NominatimResult {
  lat: string;
  lon: string;
  display_name: string;
}

interface OverpassElement {
  id: number;
  lat?: number;
  lon?: number;
  tags?: {
    name?: string;
    cuisine?: string;
    amenity?: string;
  };
}

const NOMINATIM_URL = 'https://nominatim.openstreetmap.org/search';
const OVERPASS_URL = 'https://overpass-api.de/api/interpreter';

@Injectable({
  providedIn: 'root',
})
export class LocationService {
  private readonly http = inject(HttpClient);

  readonly defaultLocation: LocationResult = {
    lat: 55.6761,
    lng: 12.5683,
    label: 'Copenhagen, Denmark',
  };

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

  // Test function to see if we can get nearby restaurants from OpenStreetMap based on lat/lng.
  getNearbyRestaurants(
    lat: number,
    lng: number,
    radius = 1000
  ): Observable<Restaurant[]> {
    const query = `
      [out:json][timeout:25];
      (
        node["amenity"="restaurant"](around:${radius},${lat},${lng});
      );
      out body;
    `;

    return this.http
      .post<{ elements: OverpassElement[] }>(OVERPASS_URL, query, {
        headers: {
          'Content-Type': 'text/plain',
        },
      })
      .pipe(
        map((data) =>
          data.elements
            .filter((element) => element.lat != null && element.lon != null)
            .map((element) => this.toRestaurant(element))
        )
      );
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

  private toRestaurant(element: OverpassElement): Restaurant {
    return {
      id: element.id,
      lat: element.lat as number,
      lng: element.lon as number,
      name: element.tags?.name ?? 'Unnamed restaurant',
      cuisine: element.tags?.cuisine,
    };
  }
}