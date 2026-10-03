import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe } from '@angular/common';
import { EMPTY, catchError, switchMap, timer } from 'rxjs';
import { AdminService } from '../core/admin.service';
import { AdminStats } from '../core/models';
import { badgeClass } from '../core/status';

const SLA_MINUTES = 45;

@Component({
  selector: 'app-admin-dashboard',
  imports: [CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
      <div class="d-flex align-items-center justify-content-between mb-4">
        <h1 class="h3 fw-semibold m-0">Operations dashboard</h1>
        <span class="small text-body-secondary">Updates every 15 seconds</span>
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }

      @if (stats(); as s) {
        <div class="row g-3">
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Total orders</div>
              <div class="fs-3 fw-semibold mt-1 ">{{ s.totalOrders }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Active now</div>
              <div class="fs-3 fw-semibold mt-1 ">{{ s.activeOrders }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Revenue (delivered)</div>
              <div class="fs-3 fw-semibold mt-1 ">{{ s.revenue | currency: 'INR' : 'symbol' : '1.0-0' }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Avg delivery time</div>
              <div class="fs-3 fw-semibold mt-1 ">{{ s.avgDeliveryMinutes === null ? 'No data' : s.avgDeliveryMinutes + ' min' }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Delivered</div>
              <div class="fs-3 fw-semibold mt-1 text-success">{{ s.deliveredOrders }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Cancelled / rejected</div>
              <div class="fs-3 fw-semibold mt-1 text-danger">{{ s.cancelledOrders }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">At risk</div>
              <div class="fs-3 fw-semibold mt-1 text-warning">{{ s.atRiskCount }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="card card-body h-100">
              <div class="small text-body-secondary">Delayed</div>
              <div class="fs-3 fw-semibold mt-1 text-danger">{{ s.delayedCount }}</div>
            </div>
          </div>
        </div>

        <section class="card card-body mt-4">
          <div class="d-flex align-items-center justify-content-between mb-3">
            <h2 class="h6 fw-medium m-0">Orders by hour of day</h2>
            @if (peakLabel(); as p) {
              <span class="small text-body-secondary">Peak: {{ p }}</span>
            }
          </div>
          <div class="d-flex align-items-end gap-1" style="height: 160px">
            @for (h of s.ordersByHour; track h.hour) {
              <div class="flex-fill h-100 d-flex flex-column justify-content-end" [title]="label(h.hour) + ': ' + h.count + ' orders'">
                <div class="w-100 rounded-top bg-primary" style="opacity: 0.8"
                     [style.height.%]="(h.count / maxCount()) * 100"
                     [style.min-height.px]="h.count > 0 ? 1 : 0"></div>
              </div>
            }
          </div>
          <div class="d-flex gap-1 mt-1 text-body-secondary" style="font-size: 10px">
            @for (h of s.ordersByHour; track h.hour) {
              <span class="flex-fill text-center">{{ h.hour % 3 === 0 ? label(h.hour) : '' }}</span>
            }
          </div>
        </section>

        <section class="card card-body mt-3">
          <h2 class="h6 fw-medium mb-1">Delay risk</h2>
          <p class="small text-body-secondary mb-3">
            Active orders open for 30+ minutes are "At risk", 45+ minutes are "Delayed" (target: {{ sla }} min).
          </p>
          @if (s.delayedOrders.length === 0) {
            <p class="small text-body-secondary mb-0">No orders at risk right now.</p>
          } @else {
            @for (d of s.delayedOrders; track d.id) {
              <div class="mb-3">
                <div class="d-flex align-items-center justify-content-between small mb-1">
                  <span>#{{ d.id }} {{ d.restaurantName }}
                    <span class="badge rounded-pill ms-1" [class]="badge(d.status)">{{ d.status }}</span>
                  </span>
                  <span [class]="d.risk === 'Delayed' ? 'text-danger' : 'text-warning'">
                    {{ d.elapsedMinutes }} min, {{ d.risk === 'Delayed' ? 'Delayed' : 'At risk' }}
                  </span>
                </div>
                <div class="progress" style="height: 8px">
                  <div class="progress-bar" [style.width.%]="pct(d.elapsedMinutes)"
                       [class]="d.risk === 'Delayed' ? 'bg-danger' : 'bg-warning'"></div>
                </div>
              </div>
            }
          }
        </section>
      } @else if (!error()) {
        <p class="text-body-secondary">Loading dashboard...</p>
      }
    </div>
  `,
})
export class AdminDashboardComponent {
  private svc = inject(AdminService);

  stats = signal<AdminStats | null>(null);
  error = signal('');

  readonly sla = SLA_MINUTES;
  readonly badge = badgeClass;

  maxCount = computed(() => Math.max(1, ...(this.stats()?.ordersByHour.map(h => h.count) ?? [0])));

  peakLabel = computed(() => {
    const hours = this.stats()?.ordersByHour ?? [];
    const top = hours.reduce((a, b) => (b.count > a.count ? b : a), { hour: 0, count: 0 });
    return top.count > 0 ? `${this.label(top.hour)} (${top.count} orders)` : '';
  });

  constructor() {
    timer(0, 15000)
      .pipe(
        switchMap(() =>
          this.svc.stats().pipe(
            catchError(() => {
              this.error.set('Could not load the dashboard. Check that the API is running.');
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(s => {
        this.stats.set(s);
        this.error.set('');
      });
  }

  label(hour: number): string {
    return `${hour % 12 === 0 ? 12 : hour % 12}${hour < 12 ? 'am' : 'pm'}`;
  }

  pct(minutes: number): number {
    return Math.min(100, (minutes / SLA_MINUTES) * 100);
  }
}