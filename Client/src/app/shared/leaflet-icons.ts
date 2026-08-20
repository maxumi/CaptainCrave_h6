import * as L from 'leaflet';

// Prevents the Leaflet icon configuration from being applied more than once.
let leafletIconsConfigured = false;


// This function replaces Leaflet's default icon paths with explicit URLs.
// otherwise it tries to fetch the marker images from the wrong location, leading to 404 errors.
export function configureLeafletDefaultIcons(): void {
  if (leafletIconsConfigured) {
    return;
  }

  delete (L.Icon.Default.prototype as L.Icon.Default & {
    _getIconUrl?: unknown;
  })._getIconUrl;

  // Set explicit URLs for Leaflet's default marker images.
  L.Icon.Default.mergeOptions({
    iconRetinaUrl:
      'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',

    iconUrl:
      'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',

    shadowUrl:
      'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  });

  leafletIconsConfigured = true;
}