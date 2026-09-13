import { TestBed } from '@angular/core/testing';
import { AppShellComponent } from './app-shell.component';
import { provideRouter, Router } from '@angular/router';
import { Component } from '@angular/core';
import { getTranslateTestingModule, getZonelessProviders } from '../../testing/spec-test-utils';
import { describe, it, expect } from 'vitest';

describe('AppShellComponent layout', () => {
  it('centers the footer container', async () => {
    await TestBed.configureTestingModule({
      imports: [AppShellComponent, getTranslateTestingModule()],
      providers: getZonelessProviders([provideRouter([])]),
    }).compileComponents();

    const fixture = TestBed.createComponent(AppShellComponent);
    // Simulate a wide viewport so max width rule applies
    const host = fixture.nativeElement as HTMLElement;
    host.style.width = '1400px';
    document.body.style.width = '1400px';
    fixture.detectChanges();

    const footerContainer = host.querySelector('.app-footer .container') as HTMLElement;
    const rect = footerContainer.getBoundingClientRect();
    const hostRect = host.getBoundingClientRect();

    const left = rect.left - hostRect.left;
    const right = hostRect.right - rect.right;
    // Centered if left and right are approximately equal
    expect(Math.abs(left - right)).toBeLessThan(2);
  });

  it('only underlines the active nav link', async () => {
    @Component({ standalone: true, template: '' })
    class DummyComponent {}

    await TestBed.configureTestingModule({
      imports: [AppShellComponent, getTranslateTestingModule()],
      providers: getZonelessProviders([
        provideRouter([
          { path: '', component: DummyComponent },
          { path: 'feedback', component: DummyComponent },
          { path: 'about', component: DummyComponent },
        ]),
      ]),
    }).compileComponents();

    const fixture = TestBed.createComponent(AppShellComponent);
    const router = TestBed.inject(Router);

    // Initial render so router directives are instantiated
    fixture.detectChanges();

    // Navigate to a non-root route
    await router.navigateByUrl('/feedback');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const links = Array.from(host.querySelectorAll('nav.nav a')) as HTMLAnchorElement[];

    // Compute which links are marked active
    const activeLinks = links.filter((a) => a.classList.contains('active'));

    // Expect exactly one active link and it should be the Feedback item
    expect(activeLinks).toHaveLength(1);
    expect(activeLinks[0].textContent?.toLowerCase()).toContain('feedback');

    // Navigate to root and assert only Generate is active
    await router.navigateByUrl('/');
    fixture.detectChanges();

    const linksAfterRoot = Array.from(host.querySelectorAll('nav.nav a')) as HTMLAnchorElement[];
    const activeAfterRoot = linksAfterRoot.filter((a) => a.classList.contains('active'));
    expect(activeAfterRoot).toHaveLength(1);
    expect(activeAfterRoot[0].textContent?.toLowerCase()).toContain('generate');
  });
});
