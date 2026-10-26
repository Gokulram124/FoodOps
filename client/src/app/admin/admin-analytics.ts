import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AnalyticsService } from '../core/analytics.service';
import { AnalyticsDashboard, LeaderboardRow, RevenueTrendPoint } from '../core/analytics.models';
import { badgeClass } from '../core/status';
import { LoaderComponent } from '../shared/loader';
@Component({
  selector: 'app-admin-analytics',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, LoaderComponent],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <h1 class="h3 fw-semibold m-0">Analytics</h1>
        <div class="btn-group" role="group" aria-label="Date range">
          @for (d of ranges; track d) {
            <button type="button" (click)="setRange(d)" class="btn btn-sm"
                    [class]="days() === d ? 'btn-primary' : 'btn-outline-secondary'">
              Last {{ d }} days
            </button>
          }
        </div>
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      @if (loading()) {
        <app-loader message="Plating your analytics..." />
      } @else if (dashboard(); as d) {
        <div class="row g-3">
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Orders</div>
              <div class="fs-3 fw-semibold mt-1">{{ d.summary.totalOrders }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Revenue</div>
              <div class="fs-3 fw-semibold mt-1">{{ d.summary.revenue | currency: 'INR' : 'symbol' : '1.0-0' }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Avg order value</div>
              <div class="fs-3 fw-semibold mt-1">{{ d.summary.avgOrderValue | currency: 'INR' : 'symbol' : '1.0-0' }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Cancelled / rejected</div>
              <div class="fs-3 fw-semibold mt-1 text-danger">{{ d.summary.cancelledOrders }}</div>
            </div>
          </div>
        </div>

        <section class="card card-body mt-4">
          <h2 class="h6 fw-medium mb-3">Revenue per day</h2>
          @if (trend().length === 0) {
            <p class="small text-body-secondary mb-0">No orders in this period.</p>
          } @else {
            <div class="d-flex align-items-end gap-1" style="height: 160px">
              @for (p of trend(); track p.period) {
                <div class="flex-fill h-100 d-flex flex-column justify-content-end"
                     [title]="(p.period | date: 'd MMM') + ': ' + (p.revenue | currency: 'INR' : 'symbol' : '1.0-0') + ' (' + p.orderCount + ' orders)'">
                  <div class="w-100 rounded-top bg-primary" style="opacity: 0.8; min-height: 2px"
                       [style.height.%]="(p.revenue / maxRevenue()) * 100"></div>
                </div>
              }
            </div>
            <div class="d-flex justify-content-between small text-body-secondary mt-2">
              <span>{{ trend()[0].period | date: 'd MMM' }}</span>
              <span>{{ trend()[trend().length - 1].period | date: 'd MMM' }}</span>
            </div>
          }
        </section>

        <div class="row g-3 mt-1">
          <div class="col-md-6">
            <section class="card card-body h-100">
              <h2 class="h6 fw-medium mb-3">Top restaurants</h2>
              @for (r of d.topRestaurants; track r.restaurantId) {
                <div class="d-flex justify-content-between small py-1">
                  <span>{{ r.name }} <span class="text-body-secondary">({{ r.orders }} orders)</span></span>
                  <span class="fw-medium">{{ r.revenue | currency: 'INR' : 'symbol' : '1.0-0' }}</span>
                </div>
              } @empty {
                <p class="small text-body-secondary mb-0">No revenue yet.</p>
              }
            </section>
          </div>
          <div class="col-md-6">
            <section class="card card-body h-100">
              <h2 class="h6 fw-medium mb-3">Orders by status</h2>
              @for (s of d.statusBreakdown; track s.status) {
                <div class="d-flex justify-content-between align-items-center small py-1">
                  <span class="badge rounded-pill" [class]="badge(s.status)">{{ s.status }}</span>
                  <span class="fw-medium">{{ s.orders }}</span>
                </div>
              }
            </section>
          </div>
        </div>

        <section class="card card-body mt-3">
          <h2 class="h6 fw-medium mb-3">Restaurant leaderboard</h2>
          <div class="table-responsive">
            <table class="table table-sm align-middle mb-0">
              <thead>
                <tr class="small text-body-secondary">
                  <th>#</th><th>Restaurant</th><th>City</th>
                  <th class="text-end">Orders</th><th class="text-end">Revenue</th>
                  <th class="text-end">Share</th><th class="text-end">City rank</th>
                </tr>
              </thead>
              <tbody>
                @for (r of leaderboard(); track r.restaurantId) {
                  <tr [class.text-body-secondary]="r.revenue === 0">
                    <td>
                      <span class="badge rounded-pill"
                            [class]="r.overallRank === 1 ? 'bg-warning-subtle text-warning-emphasis' : 'bg-secondary-subtle text-secondary-emphasis'">
                        {{ r.overallRank }}
                      </span>
                    </td>
                    <td>{{ r.name }}</td>
                    <td>{{ r.city }}</td>
                    <td class="text-end">{{ r.orders }}</td>
                    <td class="text-end">{{ r.revenue | currency: 'INR' : 'symbol' : '1.0-0' }}</td>
                    <td class="text-end">{{ r.sharePercent | number: '1.1-1' }}%</td>
                    <td class="text-end">{{ r.cityRank }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>
      }
    </div>
  `,
})
export class AdminAnalyticsComponent {
  private svc = inject(AnalyticsService);

  readonly ranges = [7, 30, 90];
  readonly badge = badgeClass;

  days = signal(30);
  loading = signal(true);
  error = signal('');
  dashboard = signal<AnalyticsDashboard | null>(null);
  leaderboard = signal<LeaderboardRow[]>([]);
  trend = signal<RevenueTrendPoint[]>([]);

  maxRevenue = computed(() => Math.max(1, ...this.trend().map(p => p.revenue)));

  constructor() {
    this.load();
  }

  setRange(days: number) {
    this.days.set(days);
    this.load();
  }

  private load() {
    const to = new Date();
    const from = new Date(to.getTime() - this.days() * 24 * 60 * 60 * 1000);

    this.loading.set(true);
    this.error.set('');

    forkJoin({
      dashboard: this.svc.dashboard(from, to),
      leaderboard: this.svc.leaderboard(from, to),
      trend: this.svc.revenueTrend(from, to, 'day'),
    }).subscribe({
      next: r => {
        this.dashboard.set(r.dashboard);
        this.leaderboard.set(r.leaderboard);
        this.trend.set(r.trend);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load analytics. Check that the API is running and you are an admin.');
        this.loading.set(false);
      },
    });
  }
}