import { ApplicationConfig, importProvidersFrom, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';

import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { API_URL, SERVER_URL } from './constants/tokens';
import { baseUrlInterceptor } from './core/http/api-url-interceptor/api-url-interceptor';
import { authInterceptor } from './core/auth/interceptor/auth-interceptor';
import { GuestTokenService } from './core/auth/guest/guest-token.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    { provide: API_URL, useValue: `${environment.serverUrl}/api` },
    { provide: SERVER_URL, useValue: environment.serverUrl },
    provideAppInitializer(() =>  {
      const guest = inject(GuestTokenService);
      return guest.ensureValidToken(true);
    }),
    provideHttpClient(withInterceptors([baseUrlInterceptor, authInterceptor ])),
    // Configure ngx-translate to load JSON files from /i18n/* via the public folder.
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        loader: {
          provide: TranslateLoader,
          useFactory: jsonLoaderFactory,
          deps: [HttpClient],
        },
        useDefaultLang: true,
      }),
    ),
  ]
};

// Minimal JSON loader for ngx-translate using HttpClient
class JsonTranslateLoader implements TranslateLoader {
  constructor(
    private readonly http: HttpClient,
    private readonly prefix = '/i18n/',
    private readonly suffix = '.json') {}

  getTranslation(lang: string) {
    return this.http.get<Record<string, unknown>>(`${this.prefix}${lang}${this.suffix}`);
  }
}

function jsonLoaderFactory(http: HttpClient) {
  return new JsonTranslateLoader(http);
}
