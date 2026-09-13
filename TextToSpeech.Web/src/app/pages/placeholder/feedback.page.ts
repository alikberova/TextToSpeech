import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-feedback-page',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <section class="container" role="region" aria-label="Feedback">
      <h2>{{ 'feedback.title' | translate }}</h2>
      <p>{{ 'feedback.body' | translate }}</p>
    </section>
  `,
})
export class FeedbackPage {}

