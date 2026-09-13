import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement, provideZonelessChangeDetection } from '@angular/core';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { OverlayContainer } from '@angular/cdk/overlay';
import { API_URL, SERVER_URL } from '../../../constants/tokens';
import { ELEVEN_LABS_KEY, ProviderKey, PROVIDERS, PROVIDER_MODELS } from '../../../constants/tts-constants';
import { HomePage } from '../home.page';
import { DEFAULT_FILE_CONTENT, DEFAULT_FILE_NAME, DEFAULT_OPENAI_VOICE_KEY } from './test-data';
import { VOICES } from '../../../core/http/endpoints';
import { Voice } from '../../../dto/voice';

export const SUBMIT_BUTTON_SELECTOR = 'button[type="submit"]';
export const DOWNLOAD_BUTTON_SELECTOR = 'button[mat-stroked-button]';
export const FORM_ERROR_SELECTOR = '.form-error';
export const SELECTOR_FORM_FIELD = 'mat-form-field.field';
export const SELECTOR_MODEL_SELECT = 'mat-select[name="model"]';
export const SELECTOR_LANGUAGE_SELECT = 'mat-select[name="language"]';
export const SELECTOR_VOICE_SELECT = 'mat-select[name="voice"]';
export const SELECTOR_PROVIDER_SELECT = 'mat-select[name="provider"]';
export const SELECTOR_MAT_OPTION = 'mat-option';
export const SELECTOR_LABELS = 'mat-form-field mat-label';
export const SELECTOR_CLEAR_BUTTON = 'button[data-testid="clearBtn"]';
export const SELECTOR_SAMPLE_PLAY_BUTTON = 'button[data-testid="samplePlayButton"]';
export const ATTR_ARIA_DISABLED = 'aria-disabled';
export const ATTR_ARIA_INVALID = 'aria-invalid';

export async function createHomeFixture(extraProviders: unknown[] = []) {
  await TestBed.resetTestingModule().configureTestingModule({
    imports: [
      HomePage,
      TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: TranslateFakeLoader }, useDefaultLang: true }),
    ],
    providers: [...getBaseProviders(), ...extraProviders],
  }).compileComponents();

  const fixture = TestBed.createComponent(HomePage);
  const component = fixture.componentInstance;
  const httpController = TestBed.inject(HttpTestingController);
  const overlayContainer = TestBed.inject(OverlayContainer);
  fixture.detectChanges();
  return { fixture, component, httpController, overlayContainer } as const;
}

export function providerKeyWithModel(): ProviderKey {
  return (PROVIDERS.find(p => Array.isArray(PROVIDER_MODELS[p.key])) || PROVIDERS[0]).key;
}

export function providerKeyWithoutModel(): ProviderKey {
  return (PROVIDERS.find(p => PROVIDER_MODELS[p.key] === null) || PROVIDERS[0]).key;
}

export function fillValidFormForOpenAI(component: HomePage): void {
  selectOpenAiMinimal(component);
  component.file.set(new File([DEFAULT_FILE_CONTENT], DEFAULT_FILE_NAME));
}

export function selectOpenAiMinimal(component: HomePage): void {
  const provider = providerKeyWithModel();
  component.onProviderChange(provider);
  component.voice.set(DEFAULT_OPENAI_VOICE_KEY);
}

// Common DOM actions
export function clickSubmit(fixture: ComponentFixture<HomePage>): void {
  fixture.debugElement.query(By.css(SUBMIT_BUTTON_SELECTOR)).nativeElement.click();
  fixture.detectChanges();
}

export function clickPlayButton(fixture: ComponentFixture<HomePage>): void {
  fixture.debugElement.query(By.css(SELECTOR_SAMPLE_PLAY_BUTTON)).nativeElement.click();
  fixture.detectChanges();
}

export function getMatErrors(fixture: ComponentFixture<HomePage>): DebugElement[] {
  return fixture.debugElement.queryAll(By.css('mat-error'));
}

export function openMatSelect(fixture: ComponentFixture<HomePage>, selector: string): void {
  const selectDebug = fixture.debugElement.query(By.css(selector));
  (selectDebug.nativeElement as HTMLElement).click();
  fixture.detectChanges();
}

export function getOverlayOptionTexts(overlayContainer: OverlayContainer): string[] {
  const container: HTMLElement = overlayContainer.getContainerElement();
  const elements = container.querySelectorAll(SELECTOR_MAT_OPTION);
  return Array.from(elements)
    .map(option => (option.textContent || '').trim())
    .filter(Boolean);
}

export function expectOneEndsWith(http: HttpTestingController, suffix: string): TestRequest {
  return http.expectOne(request => request.url.endsWith(suffix));
}

export function flushVoice(http: HttpTestingController, voices: Voice[]): void {
  const request = expectOneEndsWith(http, VOICES);
  request.flush(voices);
}

export function setProviderNarakeet(
  fixture: ComponentFixture<HomePage>,
  component: HomePage,
  http: HttpTestingController,
  voices: Voice[] = []
): void {
  const narakeet = providerKeyWithoutModel();
  component.onProviderChange(narakeet);
  flushVoice(http, voices);
  fixture.detectChanges();
}

export function setProviderElevenLabs(
  fixture: ComponentFixture<HomePage>,
  component: HomePage,
  http: HttpTestingController,
  voices: Voice[] = []
): void {
  const elevenLabsKey = PROVIDERS.find(p => p.key === ELEVEN_LABS_KEY)?.key as ProviderKey;
  component.onProviderChange(elevenLabsKey);
  flushVoice(http, voices);
  fixture.detectChanges();
}

function getBaseProviders(): unknown[] {
  return [
    provideZonelessChangeDetection(),
    provideHttpClient(),
    provideHttpClientTesting(),
    { provide: API_URL, useValue: 'https://fake-localhost:1234/api' },
    { provide: SERVER_URL, useValue: 'https://fake-localhost:1234' },
  ];
}
