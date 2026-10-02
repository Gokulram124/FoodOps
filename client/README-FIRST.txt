FoodOps Angular - Part 1 (Login, Register, JWT interceptor, role guards)

1. Copy .postcssrc.json to client\ (same place as package.json)
2. Copy the src folder contents into client\src (say Yes to replace files)
3. Delete client\src\app\app.html, app.css, app.spec.ts (if present)
4. Open src\app\core\config.ts and set API_URL port to your API's https port
5. Open src\app\app.config.ts and ADD (keep the existing providers):
     import { provideHttpClient, withInterceptors } from '@angular/common/http';
     import { authInterceptor } from './core/auth.interceptor';
     ...inside providers: [ ... , provideHttpClient(withInterceptors([authInterceptor])) ]
6. Run API from Visual Studio (F5), then in client folder: ng serve
