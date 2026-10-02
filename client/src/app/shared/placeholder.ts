import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-placeholder',
  template: `
    <div class="p-8">
      <h2 class="text-2xl font-semibold">{{ title }}</h2>
      <p class="mt-2 text-slate-400">Coming next.</p>
    </div>
  `,
})
export class PlaceholderComponent {
  title = inject(ActivatedRoute).snapshot.data['title'] ?? '';
}
