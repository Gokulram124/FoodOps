import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { Restaurant } from '../core/models';
import { RestaurantService } from '../core/restaurant.service';

@Component({
  selector: 'app-restaurant-list',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto max-w-5xl px-6 py-8">
      <div class="mb-6 flex flex-wrap items-center justify-between gap-4">
        <h1 class="text-2xl font-semibold">Restaurants</h1>
        <input [formControl]="search" placeholder="Search restaurants"
               class="w-full max-w-xs rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
      </div>

      @if (error()) {
        <div class="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="text-slate-400">Loading restaurants...</p>
      } @else if (restaurants().length === 0 && !error()) {
        <p class="text-slate-400">No restaurants match your search. Try a different name.</p>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          @for (r of restaurants(); track r.id) {
            <a [routerLink]="['/restaurants', r.id]"
               class="block rounded-2xl border border-white/10 bg-white/5 p-5 hover:border-indigo-400/50">
              <div class="flex items-start justify-between gap-2">
                <h2 class="font-medium">{{ r.name }}</h2>
                <span class="shrink-0 rounded-full px-2 py-0.5 text-xs"
                      [class]="r.isOpen ? 'bg-emerald-500/20 text-emerald-300' : 'bg-slate-500/20 text-slate-400'">
                  {{ r.isOpen ? 'Open' : 'Closed' }}
                </span>
              </div>
              <p class="mt-1 text-sm text-slate-400">{{ r.city }}</p>
              <p class="mt-3 text-sm text-slate-300">Rating {{ r.rating }}</p>
            </a>
          }
        </div>

        <div class="mt-6 flex items-center justify-center gap-4 text-sm">
          <button (click)="go(page() - 1)" [disabled]="page() === 1"
                  class="rounded-lg border border-white/10 px-3 py-1 hover:bg-white/10 disabled:opacity-40">Previous</button>
          <span class="text-slate-400">Page {{ page() }} of {{ totalPages() }}</span>
          <button (click)="go(page() + 1)" [disabled]="page() === totalPages()"
                  class="rounded-lg border border-white/10 px-3 py-1 hover:bg-white/10 disabled:opacity-40">Next</button>
        </div>
      }
    </div>
  `,
})
export class RestaurantListComponent {
  private svc = inject(RestaurantService);
  private current?: Subscription;

  search = new FormControl('', { nonNullable: true });
  restaurants = signal<Restaurant[]>([]);
  total = signal(0);
  page = signal(1);
  loading = signal(false);
  error = signal('');
  readonly pageSize = 6;

  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  constructor() {
    // Wait 400ms after typing stops, ignore if text didn't change
    this.search.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.load();
  }

  go(p: number) {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.load();
  }

  private load() {
    this.current?.unsubscribe(); // cancel the older request, so a slow old response can't overwrite a newer one
    this.loading.set(true);
    this.error.set('');

    this.current = this.svc.getAll(this.search.value.trim(), this.page(), this.pageSize).subscribe({
      next: res => {
        this.restaurants.set(res.items);
        this.total.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load restaurants. Check that the API is running.');
        this.loading.set(false);
      },
    });
  }
}