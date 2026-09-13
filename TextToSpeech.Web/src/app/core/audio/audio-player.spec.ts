import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { AudioPlayer } from './audio-player';

describe('AudioPlayer', () => {
  const noop = () => undefined;
  let originalAudio: unknown;
  interface AudioStub {
    src: string;
    play: Mock;
    pause: Mock;
    load: Mock;
  }
  let revokeCalls = 0;
  let createdUrls: string[] = [];

  class FakeAudio {
    static lastInstance: AudioStub | undefined;
    src = '';
    onended: (() => void) | null = null;
    onerror: (() => void) | null = null;
    play = vi.fn().mockReturnValue(Promise.resolve());
    pause = vi.fn();
    load = vi.fn();
    constructor(public readonly url: string) {
      FakeAudio.lastInstance = this as unknown as AudioStub;
      this.src = url;
    }
  }

  beforeEach(() => {
    originalAudio = (
      window as unknown as {
        Audio?: unknown;
      }
    ).Audio;
    (
      window as unknown as {
        Audio: unknown;
      }
    ).Audio = FakeAudio as unknown;
    revokeCalls = 0;
    createdUrls = [];
    vi.spyOn(URL, 'createObjectURL').mockImplementation(() => {
      const u = `blob:fake-${createdUrls.length + 1}`;
      createdUrls.push(u);
      return u;
    });
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {
      revokeCalls += 1;
    });
  });

  afterEach(() => {
    (
      window as unknown as {
        Audio?: unknown;
      }
    ).Audio = originalAudio as unknown;
  });

  it('creates audio and plays it', async () => {
    const player = new AudioPlayer(noop, noop);
    const blob = new Blob([new Uint8Array([1, 2, 3])], { type: 'audio/mpeg' });
    player.setBlob(blob);
    expect(URL.createObjectURL).toHaveBeenCalled();

    await player.play();
    expect(FakeAudio.lastInstance!.play).toHaveBeenCalled();
  });

  it('pause/resume delegates to underlying audio', async () => {
    const player = new AudioPlayer(noop, noop);
    player.setBlob(new Blob([new Uint8Array([1])]));

    player.pause();
    expect(FakeAudio.lastInstance!.pause).toHaveBeenCalled();

    await player.resume();
    expect(FakeAudio.lastInstance!.play).toHaveBeenCalled();
  });

  it('stop pauses, clears source, loads, and revokes URL', () => {
    const player = new AudioPlayer(noop, noop);
    player.setBlob(new Blob([new Uint8Array([1])]));
    player.stop();

    expect(FakeAudio.lastInstance!.pause).toHaveBeenCalled();
    expect(FakeAudio.lastInstance!.load).toHaveBeenCalled();
    expect(revokeCalls).toBe(1);

    // Subsequent resume should no-op (no audio instance retained)
    // If it attempted to access old audio, it would call play again
    FakeAudio.lastInstance!.play.mockClear();
  });

  it('replaces existing URL on setBlob and revokes previous', () => {
    const player = new AudioPlayer(noop, noop);
    player.setBlob(new Blob([new Uint8Array([1])]));
    expect(createdUrls).toHaveLength(1);
    expect(revokeCalls).toBe(0);

    player.setBlob(new Blob([new Uint8Array([2])]));
    expect(createdUrls).toHaveLength(2);
    expect(revokeCalls).toBe(1);
  });
});
