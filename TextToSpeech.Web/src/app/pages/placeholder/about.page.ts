import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-about-page',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <section class="container" role="region" aria-label="About">
      <h2>{{ 'about.title' | translate }}</h2>
      <p>{{ 'about.body' | translate }}</p>
    </section>
  `,
})
export class AboutPage {}

