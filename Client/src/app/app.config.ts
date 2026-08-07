import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideTransloco } from '@jsverse/transloco';
import { TranslocoHttpLoader } from '../transloco-loader';
import { providePreloadUserLang } from '../preload-user-lang';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideTransloco({
        config: { 
          availableLangs: ['en', 'da'],
          defaultLang: 'en',
          // Remove this option if your application doesn't support changing language in runtime.
          reRenderOnLangChange: true,
          prodMode: !isDevMode(),
        },
        loader: TranslocoHttpLoader
      }),
      providePreloadUserLang()
  ]
};