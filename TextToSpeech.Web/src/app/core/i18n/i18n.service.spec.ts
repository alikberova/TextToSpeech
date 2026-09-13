import { TestBed } from '@angular/core/testing';
import { I18nService } from './i18n.service';
import { TranslateService } from '@ngx-translate/core';
import { getTranslateTestingModule, getZonelessProviders } from '../../../testing/spec-test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('I18nService', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [getTranslateTestingModule()],
            providers: getZonelessProviders(),
        }).compileComponents();
    });

    it('defaults to en when storage empty', () => {
        vi.spyOn(Storage.prototype, 'getItem').mockReturnValue(null);
        const svc = TestBed.inject(I18nService);
        const translate = TestBed.inject(TranslateService);
        expect(svc.current()).toBe('en');
        expect(translate.currentLang).toBe('en');
    });

    it('initializes from localStorage when present (uk)', () => {
        vi.spyOn(Storage.prototype, 'getItem').mockReturnValue('uk');
        const svc = TestBed.inject(I18nService);
        const translate = TestBed.inject(TranslateService);
        expect(svc.current()).toBe('uk');
        expect(translate.currentLang).toBe('uk');
    });

    it('toggle switches language and stores to localStorage', () => {
        vi.spyOn(Storage.prototype, 'getItem').mockReturnValue(null);
        const setSpy = vi.spyOn(Storage.prototype, 'setItem');
        const svc = TestBed.inject(I18nService);

        expect(svc.current()).toBe('en');
        svc.toggle();
        expect(svc.current()).toBe('uk');
        expect(setSpy).toHaveBeenCalledWith('lang', 'uk');

        svc.toggle();
        expect(svc.current()).toBe('en');
        expect(setSpy).toHaveBeenCalledWith('lang', 'en');
    });

    it('set ignores unsupported languages', () => {
        vi.spyOn(Storage.prototype, 'getItem').mockReturnValue(null);
        const setSpy = vi.spyOn(Storage.prototype, 'setItem');
        const translate = TestBed.inject(TranslateService);
        const svc = TestBed.inject(I18nService);

        svc.set('en');
        expect(svc.current()).toBe('en');
        expect(translate.currentLang).toBe('en');

        // @ts-expect-error testing invalid set
        svc.set('de');
        expect(svc.current()).toBe('en');
        expect(setSpy).not.toHaveBeenCalledWith('lang', 'de');
    });
});
