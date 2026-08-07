import { inject, provideAppInitializer } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { lastValueFrom } from 'rxjs';

export function preloadLocalLang() {
  const transloco = inject(TranslocoService);

  const lang = localStorage.getItem('lang') ?? 'en';

  transloco.setActiveLang(lang);

  return lastValueFrom(transloco.load(lang));
}

export function providePreloadUserLang() {
  return provideAppInitializer(preloadLocalLang);
}