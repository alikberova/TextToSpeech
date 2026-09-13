import { describe, expect, it } from 'vitest';
import {
  createHomeFixture,
  providerKeyWithModel,
  SELECTOR_VOICE_SELECT,
  ATTR_ARIA_DISABLED,
  flushVoice,
} from './home.page.spec-setup';
import { VOICES } from './test-data';

describe('HomePage - Voice dropdown behavior', () => {
  it('voice dropdown disabled until provider selected and enables after provider/language as needed', async () => {
    const { fixture, component, httpController } = await createHomeFixture();
    const voiceSelect = fixture.nativeElement.querySelector(SELECTOR_VOICE_SELECT);
    expect(voiceSelect.getAttribute(ATTR_ARIA_DISABLED)).toBe('true');
    // No provider -> voicesForProvider empty
    expect(component.voicesForProvider()).toHaveLength(0);

    // Select provider OpenAI -> enabled and options available
    const providerWithModel = providerKeyWithModel();
    component.onProviderChange(providerWithModel);
    flushVoice(httpController, VOICES);
    fixture.detectChanges();
    const voiceSelectAfter = fixture.nativeElement.querySelector(SELECTOR_VOICE_SELECT);
    expect(voiceSelectAfter.getAttribute(ATTR_ARIA_DISABLED)).toBe('false');
    expect(component.voicesForProvider().length).toBeGreaterThan(0);
  });
});
