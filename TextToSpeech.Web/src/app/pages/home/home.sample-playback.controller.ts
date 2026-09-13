import { TtsService } from "../../core/http/tts/tts.service";
import { signal } from "@angular/core";
import { SAMPLE_PLAYBACK_ICON, SAMPLE_STATUS, SamplePlaybackIcon, SampleStatus, SampleValidationResult } from "./home.types";
import { Subscription } from "rxjs";
import { TtsRequest } from "../../dto/tts-request";
import { AudioPlayer } from "../../core/audio/audio-player";
import { UiNotifyService } from "../../core/ui/ui-notify.service";

export class SamplePlaybackController {
  private readonly samplePlayer = new AudioPlayer(
    () => this.stop(),
    () => {
      this.error.set('playback');
      this.stop();
    }
  );

  private readonly error = signal<string | null>(null);
  private readonly attempt = signal(false);
  private readonly status = signal<SampleStatus>(SAMPLE_STATUS.Stopped);
  private requestSub?: Subscription;

  readonly sampleAttempt = this.attempt.asReadonly();
  readonly sampleStatus = this.status.asReadonly();

  constructor(private readonly tts: TtsService,
    private readonly uiNotify: UiNotifyService) {}

  toggle(validateFields: () => SampleValidationResult,
    buildRequest: () => TtsRequest): void {
    if (this.handleSamplePlaybackToggle()) {
      return;
    }
    this.error.set(null);
    this.cancelRequest();
    if (!validateFields().ok) {
      this.attempt.set(true);
      this.uiNotify.error('home.errors.required');
      return;
    }
    const req = buildRequest();
    this.requestSub = this.tts.getSpeechSample(req).subscribe({
      next: (blob: Blob) => {
        this.attempt.set(false);
        this.samplePlayer.setBlob(blob);
        this.samplePlayer.play()
          .then(() => this.status.set(SAMPLE_STATUS.Playing))
          .catch((e) => {
            console.error(e);
            this.error.set('permission');
            this.status.set(SAMPLE_STATUS.Stopped);
          });
      },
      error: (e) => {
        console.error('Sample request failed', e);
        this.error.set('request');
        this.uiNotify.error('home.sample.error');
      },
      complete: () => { this.requestSub = undefined; },
    });
  }

  pause(): void {
    this.samplePlayer.pause();
    this.status.set(SAMPLE_STATUS.Paused);
  }
  
  resume(): void {
    this.samplePlayer.resume()
      .then(() => this.status.set(SAMPLE_STATUS.Playing))
      .catch((e) => {
        console.error(e);
        this.status.set(SAMPLE_STATUS.Stopped);
      });
  }

  stop(): void {
    this.cancelRequest();
    this.samplePlayer.stop();
    this.status.set(SAMPLE_STATUS.Stopped);
  }
  
  getIconName(): SamplePlaybackIcon {
    return this.status() === SAMPLE_STATUS.Playing
      ? SAMPLE_PLAYBACK_ICON.Pause
      : SAMPLE_PLAYBACK_ICON.Play;
  }

  setError(error: string | null) {
    this.error.set(error);
  }

  private cancelRequest(): void {
    if (this.requestSub) {
      try {
        this.requestSub.unsubscribe();
      } catch {
        // no-op
      }
      this.requestSub = undefined;
    }
  }

  private handleSamplePlaybackToggle(): boolean {
    const state = this.status();

    if (state === SAMPLE_STATUS.Playing) {
      this.pause();
      return true;
    }

    if (state === SAMPLE_STATUS.Paused) {
      this.resume();
      return true;
    }

    return false;
  }
}
