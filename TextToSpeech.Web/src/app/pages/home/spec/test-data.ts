import { Voice } from "../../../dto/voice";

export const PROGRESS_PROCESSING_VALUE = 40;
export const PROGRESS_VALID_VALUE = 55;
export const PROGRESS_COMPLETE_VALUE = 100;
export const DEFAULT_FILE_CONTENT = 'qwerty';
export const DEFAULT_FILE_NAME = 'any.txt';
export const DEFAULT_SAMPLE_I18N_KEY = 'home.sample.defaultText';
export const LOCALE_EN = 'en';
export const LOCALE_UK = 'uk';
export const LANGUAGE_CODE_EN_US = 'en-US';
export const LANGUAGE_CODE_EL_GR = 'el-GR';
export const LANG_I18N_PREFIX = 'languages.';
export const DEFAULT_OPENAI_VOICE_KEY = 'alloy';
export const I18N_HOME_PROVIDER_LABEL = 'home.provider.label';
export const I18N_HOME_VOICE_LABEL = 'home.voice.label';
export const I18N_HOME_PROVIDER_PLACEHOLDER = 'home.provider.placeholder';

const VOICE_1: Voice = {
  name: 'Harry',
  providerVoiceId: 'harry',
  language: null,
  qualityTier: 'Standard'
};

const VOICE_2: Voice = {
  name: 'Ben',
  providerVoiceId: 'ben',
  language: null,
  qualityTier: 'Standard'
};

const VOICE_WITH_LANG_1: Voice = {
  name: 'Melissa',
  providerVoiceId: 'melissa',
  language: { name: 'English', languageCode: LANGUAGE_CODE_EN_US },
  qualityTier: 'Standard'
};

const VOICE_WITH_LANG_2: Voice = {
  name: 'Eleni',
  providerVoiceId: 'eleni',
  language: { name: 'Greek', languageCode: LANGUAGE_CODE_EL_GR },
  qualityTier: 'Standard'
};

export const VOICES: Voice[] = [
  VOICE_1, VOICE_2,
] as const;

export const VOICES_WITH_LANG: Voice[] = [
  VOICE_WITH_LANG_1, VOICE_WITH_LANG_2,
] as const;
