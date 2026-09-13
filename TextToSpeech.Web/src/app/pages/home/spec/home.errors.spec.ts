import { beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { MatSnackBar } from '@angular/material/snack-bar';
import { createHomeFixture, fillValidFormForOpenAI, clickPlayButton, clickSubmit, selectOpenAiMinimal, expectOneEndsWith } from './home.page.spec-setup';
import { SPEECH_BASE, SPEECH_SAMPLE } from '../../../core/http/endpoints';
import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture } from '@angular/core/testing';
import { HomePage } from '../home.page';
import { AUDIO_STATUS } from '../home.types';

describe('HomePage - Error UI behavior (snackbar + state)', () => {
    let fixture: ComponentFixture<HomePage>;
    let component: HomePage;
    let httpController: HttpTestingController;

    beforeEach(async () => {
        const created = await createHomeFixture();
        fixture = created.fixture;
        component = created.component;
        httpController = created.httpController;
    });

    it('shows snackbar when sample play request fails', async () => {
        // valid minimal state for sample: provider + voice
        selectOpenAiMinimal(component);
        fixture.detectChanges();

        const snack = fixture.debugElement.injector.get(MatSnackBar);
        vi.spyOn(snack, 'open');

        clickPlayButton(fixture);

        const req = expectOneEndsWith(httpController, SPEECH_SAMPLE);
        req.flush(new Blob(['any']), { status: 500, statusText: 'Server Error' });
        fixture.detectChanges();

        expect(snack.open).toHaveBeenCalled();
        const [message] = vi.mocked((snack.open as Mock)).mock.lastCall!;
        expect(message).toBe('home.sample.error');
    });

    it('shows snackbar and sets Failed when full speech create fails', async () => {
        fillValidFormForOpenAI(component);
        fixture.detectChanges();

        const snack = fixture.debugElement.injector.get(MatSnackBar);
        vi.spyOn(snack, 'open');
        clickSubmit(fixture);

        const req = expectOneEndsWith(httpController, SPEECH_BASE);
        req.flush('no', { status: 400, statusText: 'Bad' });
        fixture.detectChanges();

        expect(component.status()).toBe(AUDIO_STATUS.Failed);
        expect(snack.open).toHaveBeenCalled();
        const [message] = vi.mocked((snack.open as Mock)).mock.lastCall!;
        expect(message).toBe('home.errors.failed');
    });

});
