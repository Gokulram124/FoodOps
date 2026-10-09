// Change the port to match your API (FoodOps.API/Properties/launchSettings.json -> https applicationUrl)
import { isDevMode } from '@angular/core';

export const API_URL = isDevMode()
  ? 'https://localhost:7197/api'
  : 'https://foodops-api.runasp.net/api';

// API origin without the /api suffix (uploaded images are served from here)
export const API_ORIGIN = API_URL.replace(/\/api$/, '');

// Backend may return a relative path like /uploads/menu/x.jpg; make it absolute
export function imageSrc(url?: string | null): string | null {
  if (!url) return null;
  return /^https?:\/\//i.test(url) ? url : `${API_ORIGIN}${url.startsWith('/') ? '' : '/'}${url}`;
}
