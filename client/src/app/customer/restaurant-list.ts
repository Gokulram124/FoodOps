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
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <h1 class="h3 fw-semibold m-0">Restaurants</h1>
        <input [formControl]="search" placeholder="Search restaurants" class="form-control w-100" style="max-width: 320px" />
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="text-body-secondary">Loading restaurants...</p>
      } @else if (restaurants().length === 0 && !error()) {
        <p class="text-body-secondary">No restaurants match your search. Try a different name.</p>
      } @else {
        <div class="row g-3">
          @for (r of restaurants(); track r.id) {
            <div class="col-12 col-sm-6 col-lg-4">
              <a [routerLink]="['/restaurants', r.id]" class="card card-hover h-100 text-decoration-none text-reset">
                <div class="card-body">
                  <div class="d-flex align-items-start justify-content-between gap-2">
                    <h2 class="h6 fw-medium m-0">{{ r.name }}</h2>
                    <span class="badge rounded-pill flex-shrink-0"
                          [class]="r.isOpen ? 'bg-success-subtle text-success-emphasis' : 'bg-secondary-subtle text-secondary-emphasis'">
                      {{ r.isOpen ? 'Open' : 'Closed' }}
                    </span>
                  </div>
                  <p class="small text-body-secondary mt-1 mb-0">{{ r.city }}</p>
                  <p class="small mt-3 mb-0">Rating {{ r.rating }}</p>
                </div>
              </a>
            </div>
          }
        </div>

        <div class="d-flex align-items-center justify-content-center gap-3 small mt-4">
          <button (click)="go(page() - 1)" [disabled]="page() === 1" class="btn btn-sm btn-outline-secondary">Previous</button>
          <span class="text-body-secondary">Page {{ page() }} of {{ totalPages() }}</span>
          <button (click)="go(page() + 1)" [disabled]="page() === totalPages()" class="btn btn-sm btn-outline-secondary">Next</button>
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