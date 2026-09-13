import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TtsService } from './tts.service';
import { TtsRequest } from '../../../dto/tts-request';
import { SPEECH_BASE, SPEECH_SAMPLE, AUDIO_DOWNLOAD } from '../endpoints';
import { getZonelessProviders } from '../../../../testing/spec-test-utils';
import { describe, beforeEach, afterEach, it, expect } from 'vitest';

describe('TtsService', () => {
    let service: TtsService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                ...getZonelessProviders(),
                provideHttpClient(),
                provideHttpClientTesting(),
            ],
        });
        service = TestBed.inject(TtsService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it('posts sample request to /speech/sample expects blob', () => {
        const inputReq = {
            ttsApi: 'openai',
            languageCode: 'en-US',
            input: 'Hello',
            ttsRequestOptions: {
                voice: { name: 'Alloy', providerVoiceId: 'alloy' },
                model: 'tts-1',
                speed: 1.2,
                responseFormat: 'mp3'
            },
        } as const;
        let received: Blob | null = null;
        service.getSpeechSample(inputReq as TtsRequest).subscribe(b => received = b);

        const r = http.expectOne(SPEECH_SAMPLE);
        expect(r.request.method).toBe('POST');
        expect(r.request.responseType).toBe('blob');
        r.flush(new Blob([new Uint8Array([1, 2, 3])], { type: 'audio/mpeg' }));
        expect(received).toBeTruthy();
    });

    it('throws if createSpeech called without file', () => {
        // file is required by the full speech generation endpoint
        const request: TtsRequest = {
            ttsApi: 'openai',
            languageCode: 'en-US',
            ttsRequestOptions: {
                voice: { name: 'Alloy', providerVoiceId: 'alloy' },
                speed: 1,
                responseFormat: 'mp3'
            },
        };
        expect(() => service.createSpeech(request)).toThrow();
    });

    it('posts multipart to /speech and returns FileId as text', () => {
        const file = new File(['x'], 'x.txt', { type: 'text/plain' });
        let id = '';
        service.createSpeech({
            ttsApi: 'openai',
            languageCode: 'en-US',
            file,
            ttsRequestOptions: {
                voice: { name: 'Alloy', providerVoiceId: 'alloy' },
                model: 'tts-1',
                speed: 1,
                responseFormat: 'mp3'
            },
        }).subscribe(v => id = v);
        const r = http.expectOne(SPEECH_BASE);
        expect(r.request.method).toBe('POST');
        // FormData check: browser serializes multipart; we can assert it is FormData by existence of get
        expect(r.request.body).toBeInstanceOf(FormData);
        const body = r.request.body as FormData;
        expect(body.get('TtsRequestOptions.Voice.ProviderVoiceId')).toBe('alloy');
        expect(body.get('TtsRequestOptions.Voice.Name')).toBe('Alloy');
        r.flush('abc-123', { status: 200, statusText: 'OK' });
        expect(id).toBe('abc-123');
    });

    it('downloads by id as blob', () => {
        let blob: Blob | null = null;
        service.downloadById('abc').subscribe(b => blob = b);
        const r = http.expectOne(`${AUDIO_DOWNLOAD}/abc`);
        expect(r.request.method).toBe('GET');
        expect(r.request.responseType).toBe('blob');
        r.flush(new Blob([new Uint8Array([1])], { type: 'audio/mpeg' }));
        expect(blob).toBeTruthy();
    });
});
