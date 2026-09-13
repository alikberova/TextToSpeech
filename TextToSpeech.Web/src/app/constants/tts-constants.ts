export const OPEN_AI_KEY = 'openai';
export const NARAKEET_KEY = 'narakeet';
export const ELEVEN_LABS_KEY = 'elevenlabs';
export const ACCEPTABLE_FILE_TYPES = ['.txt', '.pdf', '.epub'];
export const RESPONSE_FORMATS = ['mp3', 'pcm'];
export const MAX_INPUT_LENGTH = 10000000; //todo MaxSize dictionary for every type?; todo length check on backend

export const PROVIDERS = [
    { key: OPEN_AI_KEY, label: 'OpenAI' },
    { key: NARAKEET_KEY, label: 'Narakeet' },
    { key: ELEVEN_LABS_KEY, label: 'ElevenLabs' },
] as const;

export const PROVIDER_MODELS: Record<ProviderKey, string[] | null> = {
  narakeet: null,
  openai: ['gpt-4o-mini-tts', 'tts-1'],
  elevenlabs: ['eleven_multilingual_v2', 'eleven_flash_v2_5', 'eleven_turbo_v2_5'],
} as const;

export type ProviderKey = typeof PROVIDERS[number]['key'];

export const PROVIDER_RESPONSE_FORMATS: Record<ProviderKey, readonly string[]> = {
  [OPEN_AI_KEY]: RESPONSE_FORMATS,
  [NARAKEET_KEY]: ['mp3'],
  [ELEVEN_LABS_KEY]: ['mp3'],
} as const;
