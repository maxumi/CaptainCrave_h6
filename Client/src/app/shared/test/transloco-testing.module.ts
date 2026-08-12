import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';

import en from '../../../../public/i18n/en.json';
import da from '../../../../public/i18n/da.json';

export function getTranslocoModule(options: TranslocoTestingOptions = {}) {
  return TranslocoTestingModule.forRoot({
    langs: { en, da },
    translocoConfig: {
      availableLangs: ['en', 'da'],
      defaultLang: 'en',
    },
    preloadLangs: true,
    ...options,
  });
}