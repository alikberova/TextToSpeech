import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OverlayContainer } from '@angular/cdk/overlay';
import { HttpTestingController } from '@angular/common/http/testing';
import { TranslateService } from '@ngx-translate/core';
import { NARAKEET_KEY } from '../../../constants/tts-constants';
import { HomePage } from '../home.page';
import { describe, it, expect, beforeEach } from 'vitest';
import { createHomeFixture, flushVoice, openMatSelect, SELECTOR_LANGUAGE_SELECT, getOverlayOptionTexts } from './home.page.spec-setup';
import { LANG_I18N_PREFIX, LANGUAGE_CODE_EN_US, LANGUAGE_CODE_EL_GR, LOCALE_UK, VOICES_WITH_LANG, DEFAULT_SAMPLE_I18N_KEY, LOCALE_EN } from './test-data';

describe('HomePage - Translation behavior', () => {
    let fixture: ComponentFixture<HomePage>;
    let component: HomePage;
    let httpController: HttpTestingController;
    let overlayContainer: OverlayContainer;

    beforeEach(async () => {
        const created = await createHomeFixture();
        fixture = created.fixture;
        component = created.component;
        httpController = created.httpController;
        overlayContainer = created.overlayContainer;
    });

    it('falls back to i18n keys for language options when translations are missing', async () => {
        const optionTexts = act(component, httpController, fixture, overlayContainer);

        // With TranslateFakeLoader, translation returns the i18n key, not actual localized text
        expect(optionTexts.some(t => t.includes(LANG_I18N_PREFIX + LANGUAGE_CODE_EN_US))).toBe(true);
        expect(optionTexts.some(t => t.includes(LANG_I18N_PREFIX + LANGUAGE_CODE_EL_GR))).toBe(true);
    });

    it('renders translated language labels when UI language is Ukrainian', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation(LOCALE_UK, {
            languages: {
                [LANGUAGE_CODE_EN_US]: 'Англійська (американська)',
                [LANGUAGE_CODE_EL_GR]: 'Грецька'
            },
            home: { language: { placeholder: 'Оберіть мову', label: 'Мова' } }
        }, true);
        translate.use(LOCALE_UK);

        const optionTexts = act(component, httpController, fixture, overlayContainer);

        expect(optionTexts.some(t => t.includes('Грецька'))).toBe(true);
        expect(optionTexts.some(t => t.includes('Англійська (американська)'))).toBe(true);
    });

    it('initializes sampleText from i18n key with FakeLoader and respects user edits on language change', async () => {
        // With TranslateFakeLoader, translations echo keys
        expect(component.sampleText()).toBe(DEFAULT_SAMPLE_I18N_KEY);

        const userText = 'My personal sample input';
        component.onSampleTextInput(userText);
        fixture.detectChanges();
        // Trigger language change and ensure it does not overwrite user edits
        const translate = fixture.debugElement.injector.get(TranslateService) as TranslateService;
        translate.use(LOCALE_UK);
        expect(component.sampleText()).toBe(userText);

        // If user clears back to empty (treated as not edited), next language change applies default again
        component.onSampleTextInput('');
        fixture.detectChanges();
        translate.use(LOCALE_EN);
        fixture.detectChanges();
        expect(component.sampleText()).toBe(DEFAULT_SAMPLE_I18N_KEY);
    });

    it('sorts language options by translated display text', () => {
        const translate = TestBed.inject(TranslateService);

        translate.setTranslation(LOCALE_EN, {
            languages: {
            [LANGUAGE_CODE_EN_US]: 'Zulu',
            [LANGUAGE_CODE_EL_GR]: 'Alpha'
            },
            home: { language: { placeholder: 'Pick', label: 'Language' } }
        }, true);

        translate.use(LOCALE_EN);

        const optionTexts = act(component, httpController, fixture, overlayContainer);

        const alphaIndex = optionTexts.findIndex(t => t.includes('Alpha'));
        const zuluIndex = optionTexts.findIndex(t => t.includes('Zulu'));

        expect(alphaIndex).toBeGreaterThan(-1);
        expect(zuluIndex).toBeGreaterThan(-1);
        expect(alphaIndex).toBeLessThan(zuluIndex);
    });

});

function act(component: HomePage, httpController: HttpTestingController, fixture: ComponentFixture<HomePage>, overlayContainer: OverlayContainer): string[] {
    component.onProviderChange(NARAKEET_KEY);
    flushVoice(httpController, VOICES_WITH_LANG);
    fixture.detectChanges();

    // Open the language select to render options into the overlay
    openMatSelect(fixture, SELECTOR_LANGUAGE_SELECT);

    return getOverlayOptionTexts(overlayContainer);
}
