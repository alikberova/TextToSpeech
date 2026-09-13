import type { Voice } from '../../dto/voice';
import { AUDIO_STATUS, LangSelectOption, type AudioStatus, type SelectOption } from './home.types';

// Build language select options from voices that include language metadata
export function getLanguagesFromVoices(voices: readonly Voice[]): LangSelectOption[] {
  const map = new Map<string, string>();
  for (const v of voices) {
    const code = v.language?.languageCode;
    if (code && !map.has(code)) {
      map.set(code, `languages.${code}`);
    }
  }
  return Array.from(map.entries())
    .map(([key, label]) => ({ key, label, displayText: '' }))
}

// Build voices options based on provider and optional language
export function getVoicesForProvider(
  providerKey: string | undefined,
  languageCode: string | undefined,
  voices: readonly Voice[],
): readonly SelectOption[] {
  if (!providerKey) {
    return [] as const;
  }
  const filtered = voices.filter(v => !languageCode || v.language?.languageCode === languageCode);
  return filtered.map(v => ({ key: v.providerVoiceId, label: capitalizeFirstLetter(v.name) }));
}

// Map server status to an appropriate Material icon name
export function mapStatusToIcon(status: AudioStatus): string {
  switch (status) {
    case AUDIO_STATUS.Created:
      return 'schedule';
    case AUDIO_STATUS.Processing:
      return 'autorenew';
    case AUDIO_STATUS.Completed:
      return 'check_circle';
    case AUDIO_STATUS.Failed:
      return 'error';
    case AUDIO_STATUS.Canceled:
      return 'cancel';
    case AUDIO_STATUS.Idle:
    default:
      return 'info';
  }
}

// Build a filename for download given original name and selected format
export function buildDownloadFilename(originalName: string, extension: string): string {
  const dotIndex = originalName.lastIndexOf('.');
  const baseName = dotIndex > -1 ? originalName.substring(0, dotIndex) : originalName;
  return `${baseName}.${extension}`;
}

function capitalizeFirstLetter(text: string): string {
  if (!text) {
    return text;
  }
  return text[0].toUpperCase() + text.slice(1);
}