// Shared types for HomePage and related helpers
// Keep strictly typed and reusable across tests.

export const AUDIO_STATUS = {
  Idle: 'Idle',
  Created: 'Created',
  Processing: 'Processing',
  Completed: 'Completed',
  Failed: 'Failed',
  Canceled: 'Canceled',
} as const;

export type AudioStatus = typeof AUDIO_STATUS[keyof typeof AUDIO_STATUS];

export const SAMPLE_STATUS = {
  Stopped: 'stopped',
  Playing: 'playing',
  Paused: 'paused',
} as const;

export type SampleStatus = typeof SAMPLE_STATUS[keyof typeof SAMPLE_STATUS];

// Material icon names
export const SAMPLE_PLAYBACK_ICON = {
  Pause: 'pause_circle',
  Play: 'play_circle',
} as const;

export type SamplePlaybackIcon = typeof SAMPLE_PLAYBACK_ICON[keyof typeof SAMPLE_PLAYBACK_ICON];

export interface SelectOption {
  key: string;
  label: string;
}

export interface LangSelectOption extends SelectOption {
  displayText: string; // resolved, human-readable language name
}

export type FieldKey = 'provider' | 'model' | 'language' | 'voice' | 'file';

export type SampleValidationResult =
  | { ok: true }
  | { ok: false };