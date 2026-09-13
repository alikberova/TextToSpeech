import { SignalRService } from './signalr.service';
import * as signalR from '@microsoft/signalr';
import { createHubWithOnSpy, setSignalRHub } from '../../../testing/signalr-test-utils';
import { AudioStatusCallback } from './audio-status-callback';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { GuestTokenService } from '../auth/guest/guest-token.service';

describe('SignalRService', () => {
    const fileId = 'id-123';
    let service: SignalRService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                SignalRService,
                { provide: GuestTokenService, useValue: { getToken: vi.fn(() => 'token') } },
            ],
        });
        service = TestBed.inject(SignalRService);
    });

    it('registers AudioStatusUpdated listener and normalizes progress', () => {
        let registeredCallback: AudioStatusCallback | undefined;
        const onSpy = vi.fn().mockImplementation((evt: string, cb: AudioStatusCallback) => {
            if (evt === 'AudioStatusUpdated') {
                registeredCallback = cb;
            }
        });

        const partialHub = createHubWithOnSpy(onSpy);
        setSignalRHub(service, partialHub);

        const calls: {
            id: string;
            status: string;
            progress: number | null;
            error?: string;
        }[] = [];
        service.addAudioStatusListener((id, st, pr, err) => calls.push({ id, status: st, progress: pr, error: err }));

        expect(onSpy).toHaveBeenCalledWith('AudioStatusUpdated', expect.any(Function));

        // Simulate hub events
        registeredCallback!(fileId, 'Processing', 42, undefined);
        registeredCallback!('id-2', 'Processing', undefined as unknown as number, 'bad');

        expect(calls).toHaveLength(2);
        expect(calls[0]).toEqual({ id: fileId, status: 'Processing', progress: 42, error: undefined });
        expect(calls[1]).toEqual({ id: 'id-2', status: 'Processing', progress: null, error: 'bad' });
    });

    it('invokes CancelProcessing on cancel()', async () => {
        const fakeHub = {
            invoke: vi.fn().mockReturnValue(Promise.resolve())
        } as unknown as signalR.HubConnection;
        setSignalRHub(service, fakeHub);

        service.cancelProcessing(fileId);
        expect(fakeHub.invoke).toHaveBeenCalledWith('CancelProcessing', fileId);
    });

    it('stops and clears hub on stopConnection()', async () => {
        const fakeHub = {
            stop: vi.fn().mockReturnValue(Promise.resolve())
        } as unknown as signalR.HubConnection;
        setSignalRHub(service, fakeHub);

        service.stopConnection();
        expect(fakeHub.stop).toHaveBeenCalled();
        expect((service as unknown as {
            hub?: signalR.HubConnection;
        }).hub).toBeUndefined();
    });
});
