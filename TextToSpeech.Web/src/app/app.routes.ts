import { Routes } from '@angular/router';

// Application routes are lazy by default. For now we only have Home.
export const routes: Routes = [
  {
    path: '',
    title: 'Home | TTS',
    loadComponent: () =>
      import('./pages/home/home.page').then((m) => m.HomePage),
  },
  // Future routes; keep these lazy placeholders for nav consistency.
  {
    path: 'feedback',
    title: 'Feedback | TTS',
    loadComponent: () =>
      import('./pages/placeholder/feedback.page').then((m) => m.FeedbackPage),
  },
  {
    path: 'about',
    title: 'About | TTS',
    loadComponent: () =>
      import('./pages/placeholder/about.page').then((m) => m.AboutPage),
  },
  { path: '**', redirectTo: '' },
];
