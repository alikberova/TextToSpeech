import { inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

// Runtime i18n service. Stores the current language in localStorage and
// initializes ngx-translate with default language 'en'.
@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly storageKey = 'lang';
  private readonly translate = inject(TranslateService);

  private readonly supported = ['en', 'uk'] as const;
  readonly current = signal<'en' | 'uk'>(this.readLang());

  constructor() {
    const lang = this.current();
    this.translate.addLangs(this.supported);
    this.translate.setDefaultLang('en');
    this.translate.use(lang);
  }

  set(lang: 'en' | 'uk') {
    if (!this.supported.includes(lang)) return;
    this.current.set(lang);
    localStorage.setItem(this.storageKey, lang);
    this.translate.use(lang);
  }

  toggle() {
    this.set(this.current() === 'en' ? 'uk' : 'en');
  }

  private readLang(): 'en' | 'uk' {
    const raw = localStorage.getItem(this.storageKey);
    return raw === 'uk' ? 'uk' : 'en';
  }
}

