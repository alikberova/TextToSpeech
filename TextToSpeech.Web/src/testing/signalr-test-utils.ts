import * as signalR from '@microsoft/signalr';
import { SignalRService } from '../app/core/realtime/signalr.service';
import { AudioStatusCallback } from '../app/core/realtime/audio-status-callback';

/**
 * Assigns a mock HubConnection to the SignalRService private field for testing.
 */
export function setSignalRHub(service: SignalRService, hub: Partial<signalR.HubConnection>): void {
  (service as unknown as { hub?: Partial<signalR.HubConnection> }).hub = hub;
}

/**
 * Creates a Partial HubConnection that wires the provided spy to the `on` callback.
 */
export function createHubWithOnSpy(
  onSpy: (event: string, cb: AudioStatusCallback) => unknown
): Partial<signalR.HubConnection> {
  return {
    on: onSpy as unknown as signalR.HubConnection['on'],
  };
}

/**
 * Simple stub used to simulate SignalRService callbacks in component specs.
 */
export class SignalRStub {
  private callback?: AudioStatusCallback;

  startConnection(): void {
    return;
  }

  addAudioStatusListener(callback: AudioStatusCallback): void {
    this.callback = callback;
  }

  cancelProcessing(): void {
    return;
  }

  trigger(id: string, status: string, progress?: number | null, error?: string): void {
    this.callback?.(id, status, progress ?? null, error);
  }
}
