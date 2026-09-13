import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideRouter } from '@angular/router';
import { getTranslateTestingModule, getZonelessProviders } from '../testing/spec-test-utils';
import { describe, beforeEach, it, expect } from 'vitest';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        App,
        getTranslateTestingModule(),
      ],
      providers: getZonelessProviders([provideRouter([])]),
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render product name in toolbar', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    // Brand element contains product name
    expect(compiled.querySelector('.product')?.textContent).toContain('TTS Studio');
  });
});
