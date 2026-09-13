import { createHomeFixture } from './home.page.spec-setup';
import { AUDIO_STATUS, type AudioStatus } from '../home.types';
import { describe, expect, it } from "vitest";

describe('HomePage - Icon mapping', () => {
  it('progressIcon maps statuses to material icons', async () => {
    const { component } = await createHomeFixture();
    const cases: { status: AudioStatus; icon: string }[] = [
      { status: AUDIO_STATUS.Idle, icon: 'info' },
      { status: AUDIO_STATUS.Created, icon: 'schedule' },
      { status: AUDIO_STATUS.Processing, icon: 'autorenew' },
      { status: AUDIO_STATUS.Completed, icon: 'check_circle' },
      { status: AUDIO_STATUS.Failed, icon: 'error' },
      { status: AUDIO_STATUS.Canceled, icon: 'cancel' },
    ];
    for (const c of cases) {
      component.status.set(c.status);
      expect(component.progressIcon()).toBe(c.icon);
    }
  });
});
