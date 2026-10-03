// Change the port to match your API (FoodOps.API/Properties/launchSettings.json -> https applicationUrl)
import { isDevMode } from '@angular/core';

export const API_URL = isDevMode()
  ? 'https://localhost:7197/api'
  : 'https://foodops-api.runasp.net/api';