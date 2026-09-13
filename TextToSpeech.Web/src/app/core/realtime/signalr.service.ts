import { inject, Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { SERVER_URL } from '../../constants/tokens';
import { AudioStatusCallback } from './audio-status-callback';
import { GuestTokenService } from '../auth/guest/guest-token.service';
import { environment } from '../../../environments/environment';

/** Wrapper around SignalR hub for audio generation status updates. */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly guestTokenService = inject(GuestTokenService);
  private hub?: signalR.HubConnection;

  startConnection() {
    if (this.hub && this.hub.state === signalR.HubConnectionState.Connected) return;
    const audioHubUrl = `${inject(SERVER_URL)}/audioHub`;
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(audioHubUrl, {
          accessTokenFactory: () => this.guestTokenService.getToken() ?? '',
        })
      .configureLogging(environment.name === 'development'
        ? signalR.LogLevel.Information
        : signalR.LogLevel.Error)
      .withAutomaticReconnect()
      .build();
    this.hub.start().catch((err: unknown) => console.error('SignalR start error', err));
  }

  stopConnection() {
    if (!this.hub) {
      return;
    }
    const currentHub = this.hub;
    this.hub = undefined;
    currentHub.stop().catch((err: unknown) => console.error('SignalR stop error', err));
  }

  addAudioStatusListener(callback: AudioStatusCallback) {
    this.hub?.on('AudioStatusUpdated', (fileId: string, status: string, progress?: number | null, errorMessage?: string) => {
      callback(fileId, status, progress ?? null, errorMessage);
    });
  }

  cancelProcessing(fileId: string) {
    if (!this.hub) {
      return;
    }
    this.hub.invoke('CancelProcessing', fileId).catch((err: unknown) => console.error('SignalR invoke error', err));
  }
}
