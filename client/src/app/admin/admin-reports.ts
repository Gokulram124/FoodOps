import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { AdminService } from '../core/admin.service';
import { AdminRestaurant, OwnerSummary, RevenuePoint } from '../core/models';

type Period = 'day' | 'week' | 'month' | 'year';

@Component({
  selector: 'app-admin-reports',
  imports: [CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
            <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
        <h1 class="h3 fw-semibold m-0">Revenue report</h1>
        <div class="d-flex gap-2">
          <div class="btn-group btn-group-sm">
            @for (p of periods; track p) {
              <button type="button" class="btn" [class]="period() === p ? 'btn-primary' : 'btn-outline-secondary'"
                      (click)="setPeriod(p)">{{ p }}</button>
            }
          </div>
          <button type="button" class="btn btn-sm btn-success" (click)="download()">Download Excel</button>
        </div>
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      <div class="row g-3 mb-3">
        <div class="col-md-6">
          <label class="form-label small">Owner</label>
          <select class="form-select" (change)="setOwner($any($event.target).value)">
            <option value="">All owners</option>
            @for (o of owners(); track o.id) {
              <option [value]="o.id">{{ o.fullName }}</option>
            }
          </select>
        </div>
        <div class="col-md-6">
          <label class="form-label small">Restaurant</label>
          <select class="form-select" (change)="setRestaurant($any($event.target).value)">
            <option value="">All restaurants</option>
            @for (r of visibleRestaurants(); track r.id) {
              <option [value]="r.id">{{ r.name }} ({{ r.city }})</option>
            }
          </select>
        </div>
      </div>

      <div class="row g-3">
        <div class="col-4">
          <div class="card card-body h-100">
            <div class="small text-body-secondary">Revenue</div>
            <div class="fs-4 fw-semibold">{{ totalRevenue() | currency: 'INR' : 'symbol' : '1.0-0' }}</div>
          </div>
        </div>
        <div class="col-4">
          <div class="card card-body h-100">
            <div class="small text-body-secondary">Delivered orders</div>
            <div class="fs-4 fw-semibold">{{ totalOrders() }}</div>
          </div>
        </div>
        <div class="col-4">
          <div class="card card-body h-100">
            <div class="small text-body-secondary">Avg order value</div>
            <div class="fs-4 fw-semibold">{{ avgOrder() | currency: 'INR' : 'symbol' : '1.0-0' }}</div>
          </div>
        </div>
      </div>

      <section class="card card-body mt-3">
        <h2 class="h6 fw-medium mb-3">Revenue by {{ period() }}</h2>
        <div class="d-flex align-items-end gap-1" style="height: 180px">
          @for (p of points(); track p.label) {
            <div class="flex-fill h-100 d-flex flex-column justify-content-end"
                 [title]="p.label + ': ' + (p.revenue | currency: 'INR' : 'symbol' : '1.0-0') + ' (' + p.orders + ' orders)'">
              <div class="w-100 rounded-top bg-primary" style="opacity: 0.8"
                   [style.height.%]="(p.revenue / maxRevenue()) * 100"
                   [style.min-height.px]="p.revenue > 0 ? 2 : 0"></div>
            </div>
          }
        </div>
        <div class="d-flex gap-1 mt-1 text-body-secondary" style="font-size: 10px">
          @for (p of points(); track p.label) {
            <span class="flex-fill text-center text-truncate">{{ p.label }}</span>
          }
        </div>
      </section>

      <div class="card mt-3">
        <table class="table mb-0 align-middle">
          <thead>
            <tr><th>Period</th><th class="text-end">Orders</th><th class="text-end">Revenue</th></tr>
          </thead>
          <tbody>
            @for (p of points(); track p.label) {
              <tr>
                <td>{{ p.label }}</td>
                <td class="text-end">{{ p.orders }}</td>
                <td class="text-end">{{ p.revenue | currency: 'INR' : 'symbol' : '1.0-0' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class AdminReportsComponent {
  private svc = inject(AdminService);

  readonly periods: Period[] = ['day', 'week', 'month', 'year'];
  period = signal<Period>('day');
  restaurantId = signal<number | null>(null);
  ownerId = signal<string | null>(null);

  points = signal<RevenuePoint[]>([]);
  owners = signal<OwnerSummary[]>([]);
  restaurants = signal<AdminRestaurant[]>([]);
  error = signal('');

  visibleRestaurants = computed(() => {
    const owner = this.ownerId();
    return owner ? this.restaurants().filter(r => r.ownerId === owner) : this.restaurants();
  });

  totalRevenue = computed(() => this.points().reduce((s, p) => s + p.revenue, 0));
  totalOrders = computed(() => this.points().reduce((s, p) => s + p.orders, 0));
  avgOrder = computed(() => (this.totalOrders() ? this.totalRevenue() / this.totalOrders() : 0));
  maxRevenue = computed(() => Math.max(1, ...this.points().map(p => p.revenue)));

  constructor() {
    this.svc.owners().subscribe(o => this.owners.set(o));
    this.svc.restaurants().subscribe(r => this.restaurants.set(r));
    this.load();
  }

  setPeriod(p: Period) {
    this.period.set(p);
    this.load();
  }

  setOwner(id: string) {
    this.ownerId.set(id || null);
    this.restaurantId.set(null);   // the restaurant list changes with the owner
    this.load();
  }

  setRestaurant(id: string) {
    this.restaurantId.set(id ? Number(id) : null);
    this.load();
  }

  load() {
    this.svc.revenue(this.period(), this.restaurantId(), this.ownerId()).subscribe({
      next: p => { this.points.set(p); this.error.set(''); },
      error: () => this.error.set('Could not load the report.'),
    });
  }
    download() {
    this.svc.revenueExcel(this.period(), this.restaurantId(), this.ownerId()).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `revenue-${this.period()}.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.error.set('Could not download the report.'),
    });
  }
}