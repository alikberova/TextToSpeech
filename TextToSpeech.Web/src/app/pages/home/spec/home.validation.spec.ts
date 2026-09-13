import { ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgModel } from '@angular/forms';
import { createHomeFixture, providerKeyWithModel, providerKeyWithoutModel, FORM_ERROR_SELECTOR, SELECTOR_MODEL_SELECT, SELECTOR_FORM_FIELD, ATTR_ARIA_INVALID, getMatErrors, clickSubmit, clickPlayButton, setProviderNarakeet, selectOpenAiMinimal, fillValidFormForOpenAI, flushVoice } from './home.page.spec-setup';
import { HttpTestingController } from '@angular/common/http/testing';
import { ProviderKey } from '../../../constants/tts-constants';
import { HomePage } from '../home.page';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DEFAULT_FILE_CONTENT, DEFAULT_FILE_NAME, LANGUAGE_CODE_EN_US, VOICES_WITH_LANG } from './test-data';

describe('HomePage - Validation UX', () => {
    // ViewChild property keys used to focus fields
    const REF_NAMES = {
        model: 'modelEl',
        provider: 'providerEl',
        voice: 'voiceEl',
        language: 'languageEl'
    } as const;

    type RefName = typeof REF_NAMES[keyof typeof REF_NAMES];

    function getRef(component: HomePage, name: RefName): {
        focus: () => void;
    } {
        const refs = component as unknown as Record<RefName, {
            focus: () => void;
        }>;
        return refs[name];
    }

    let fixture: ComponentFixture<HomePage>;
    let component: HomePage;
    let httpController: HttpTestingController;

    beforeEach(async () => {
        const created = await createHomeFixture();
        fixture = created.fixture;
        component = created.component;
        httpController = created.httpController;
    });

    it('shows no errors on initial load', async () => {
        expect(getMatErrors(fixture)).toHaveLength(0);
        expect(fixture.debugElement.query(By.css(FORM_ERROR_SELECTOR))).toBeNull();
    });

    it('shows a field error after it is touched', async () => {
        const providerNgModel = fixture.debugElement.queryAll(By.directive(NgModel))
            .map(el => el.injector.get(NgModel))
            .find((m: NgModel) => m.name === 'provider') as NgModel;
        providerNgModel.control.markAsTouched();
        fixture.detectChanges();
        expect(getMatErrors(fixture)).toHaveLength(1);
    });

    it('after a failed submit, invalid fields display errors and form-level error appears', async () => {
        clickSubmit(fixture);
        expect(getMatErrors(fixture).length).toBeGreaterThanOrEqual(2);
        expect(fixture.debugElement.query(By.css(FORM_ERROR_SELECTOR))).not.toBeNull();
    });

    it('a field error disappears once it becomes valid', async () => {
        clickSubmit(fixture);
        expect(getMatErrors(fixture).length).toBeGreaterThan(0);

        const providerWithoutModel = providerKeyWithoutModel();
        component.onProviderChange(providerWithoutModel);
        flushVoice(httpController, []);
        fixture.detectChanges();

        expect(getMatErrors(fixture).length).toBeLessThan(4);
    });

    it('form-level error hides after all fields become valid', async () => {
        clickSubmit(fixture);
        expect(fixture.debugElement.query(By.css(FORM_ERROR_SELECTOR))).not.toBeNull();

        fillValidFormForOpenAI(component);
        fixture.detectChanges();

        expect(fixture.debugElement.query(By.css(FORM_ERROR_SELECTOR))).toBeNull();
    });

    it('requires model only when provider needs it and selects first automatically', async () => {
        // Provider that requires model -> model auto-selected; no model error after submit
        // language not required for OpenAI
        selectOpenAiMinimal(component);
        fixture.detectChanges();
        clickSubmit(fixture);

        // With provider requiring model, requiresModel() is true and model field is present & valid
        expect(component.requiresModel()).toBe(true);
        const formFields = fixture.debugElement.queryAll(By.css(SELECTOR_FORM_FIELD));
        const modelFieldEl = formFields.find(fields => !!fields.query(By.css(SELECTOR_MODEL_SELECT)));
        expect(modelFieldEl).toBeTruthy();
        expect(modelFieldEl!.attributes[ATTR_ARIA_INVALID]).not.toBe('true');

        // Switch to provider that does NOT require model -> model field disappears
        component.onProviderChange(providerKeyWithoutModel());
        fixture.detectChanges();

        const formFields2 = fixture.debugElement.queryAll(By.css(SELECTOR_FORM_FIELD));
        const modelFieldGone = formFields2.find(ff => !!ff.query(By.css(SELECTOR_MODEL_SELECT)));
        expect(component.requiresModel()).toBe(false);
        expect(modelFieldGone).toBeUndefined();
    });

    it('focusFirstInvalid focuses Voice when provider requires model but model is auto-selected', async () => {
        const providerWithModel = providerKeyWithModel();
        component.onProviderChange(providerWithModel);
        component.voice.set('');
        component.file.set(new File([DEFAULT_FILE_CONTENT], DEFAULT_FILE_NAME));
        fixture.detectChanges();

        const modelRef = getRef(component, REF_NAMES.model);
        const providerRef = getRef(component, REF_NAMES.provider);
        const voiceRef = getRef(component, REF_NAMES.voice);
        vi.spyOn(modelRef, 'focus');
        vi.spyOn(providerRef, 'focus');
        vi.spyOn(voiceRef, 'focus');

        component.submit();
        await fixture.whenStable();

        expect(voiceRef.focus).toHaveBeenCalled();
        expect(providerRef.focus).not.toHaveBeenCalled();
        expect(modelRef.focus).not.toHaveBeenCalled();
    });

    it('submit speech in Narakeet flow focuses missing Voice', async () => {
        setProviderNarakeet(fixture, component, httpController, VOICES_WITH_LANG);
        component.onLanguageChange(LANGUAGE_CODE_EN_US);
        component.voice.set('');
        component.file.set(new File([DEFAULT_FILE_CONTENT], DEFAULT_FILE_NAME));
        fixture.detectChanges();

        const providerRef = getRef(component, REF_NAMES.provider);
        const voiceRef = getRef(component, REF_NAMES.voice);
        vi.spyOn(voiceRef, 'focus');
        vi.spyOn(providerRef, 'focus');

        component.submit();
        await fixture.whenStable();

        expect(voiceRef.focus).toHaveBeenCalled();
        expect(providerRef.focus).not.toHaveBeenCalled();
    });

    it('play sample in Narakeet flow focuses missing language then voice', async () => {
        // Pick a provider that has no models (Narakeet-like)
        setProviderNarakeet(fixture, component, httpController);
        component.language.set('');
        component.voice.set('');
        fixture.detectChanges();

        const languageRef = getRef(component, 'languageEl');
        const voiceRef = getRef(component, 'voiceEl');
        vi.spyOn(languageRef, 'focus');
        vi.spyOn(voiceRef, 'focus');

        // First click -> language missing
        clickPlayButton(fixture);
        await fixture.whenStable();
        expect(languageRef.focus).toHaveBeenCalled();

        // Provide language, keep voice missing -> next click focuses voice
        component.language.set('en-US');
        fixture.detectChanges();
        clickPlayButton(fixture);
        await fixture.whenStable();
        expect(voiceRef.focus).toHaveBeenCalled();
    });

    it('play sample with missing fields focuses first missing and flags attempt', async () => {
        // Ensure no provider selected
        component.provider.set('' as unknown as ProviderKey);
        fixture.detectChanges();

        const providerRef = getRef(component, 'providerEl');
        vi.spyOn(providerRef, 'focus');

        clickPlayButton(fixture);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(component.sampleAttempt()).toBe(true);
        expect(providerRef.focus).toHaveBeenCalled();
    });

});
