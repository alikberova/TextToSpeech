import { By } from '@angular/platform-browser';
import { createHomeFixture, fillValidFormForOpenAI, DOWNLOAD_BUTTON_SELECTOR, clickSubmit, expectOneEndsWith } from './home.page.spec-setup';
import { SPEECH_BASE } from '../../../core/http/endpoints';
import { SignalRService } from '../../../core/realtime/signalr.service';
import { SignalRStub } from '../../../../testing/signalr-test-utils';
import { AUDIO_STATUS } from '../home.types';
import { describe, it, expect } from 'vitest';
import { PROGRESS_PROCESSING_VALUE, PROGRESS_COMPLETE_VALUE, PROGRESS_VALID_VALUE } from './test-data';

describe('HomePage - SignalR and Progress', () => {
    it('enables Download button when SignalR reports Completed', async () => {
        const signalRStub = new SignalRStub();

        const { fixture, component, httpController } = await createHomeFixture([
            { provide: SignalRService, useValue: signalRStub },
        ]);

        fillValidFormForOpenAI(component);
        fixture.detectChanges();

        clickSubmit(fixture);
        const testRequest = expectOneEndsWith(httpController, SPEECH_BASE);
        const formdata = testRequest.request.body as FormData;
        expect(formdata.get('TtsRequestOptions.ResponseFormat')).toBe('mp3');
        testRequest.flush('id-1');

        signalRStub.trigger('id-1', AUDIO_STATUS.Processing, PROGRESS_PROCESSING_VALUE);
        fixture.detectChanges();
        expect(component.status()).toBe(AUDIO_STATUS.Processing);
        signalRStub.trigger('id-1', AUDIO_STATUS.Completed, PROGRESS_COMPLETE_VALUE);
        fixture.detectChanges();
        expect(component.status()).toBe(AUDIO_STATUS.Completed);
        const downloadButton = fixture.debugElement.query(By.css(DOWNLOAD_BUTTON_SELECTOR));
        expect(downloadButton.properties['disabled']).toBe(false);
    });

    it('updates progress when valid and ignores invalid values', async () => {
        const stub = new SignalRStub();

        const { fixture, component, httpController } = await createHomeFixture([
            { provide: SignalRService, useValue: stub },
        ]);

        fillValidFormForOpenAI(component);
        fixture.detectChanges();

        clickSubmit(fixture);
        const testRequest = expectOneEndsWith(httpController, SPEECH_BASE);
        testRequest.flush('file-xyz');

        stub.trigger('file-xyz', AUDIO_STATUS.Processing, PROGRESS_VALID_VALUE);
        fixture.detectChanges();
        expect(component.progress()).toBe(PROGRESS_VALID_VALUE);

        stub.trigger('file-xyz', AUDIO_STATUS.Processing, Number.NaN);
        fixture.detectChanges();
        expect(component.progress()).toBe(PROGRESS_VALID_VALUE);
    });
});
