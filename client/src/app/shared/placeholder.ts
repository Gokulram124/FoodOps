import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-placeholder',
  template: `
    <div class="p-4">
      <h2 class="h3 fw-semibold">{{ title }}</h2>
      <p class="mt-2 text-body-secondary">Coming next.</p>
    </div>
  `,
})
export class PlaceholderComponent {
  title = inject(ActivatedRoute).snapshot.data['title'] ?? '';
}
