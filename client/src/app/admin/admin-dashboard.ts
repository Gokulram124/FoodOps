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
    <div class="mx-auto max-w-5xl px-6 py-8">
      <div class="mb-6 flex items-center justify-between">
        <h1 class="text-2xl font-semibold">Operations dashboard</h1>
        <span class="text-xs text-slate-500">Updates every 15 seconds</span>
      </div>

      @if (error()) {
        <div class="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
      }

      @if (stats(); as s) {
        <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Total orders</p>
            <p class="mt-1 text-2xl font-semibold">{{ s.totalOrders }}</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Active now</p>
            <p class="mt-1 text-2xl font-semibold">{{ s.activeOrders }}</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Revenue (delivered)</p>
            <p class="mt-1 text-2xl font-semibold">{{ s.revenue | currency: 'INR' : 'symbol' : '1.0-0' }}</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Avg delivery time</p>
            <p class="mt-1 text-2xl font-semibold">
              {{ s.avgDeliveryMinutes === null ? 'No data' : s.avgDeliveryMinutes + ' min' }}
            </p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Delivered</p>
            <p class="mt-1 text-2xl font-semibold text-emerald-300">{{ s.deliveredOrders }}</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Cancelled / rejected</p>
            <p class="mt-1 text-2xl font-semibold text-red-300">{{ s.cancelledOrders }}</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">At risk</p>
            <p class="mt-1 text-2xl font-semibold text-amber-300">{{ s.atRiskCount }}</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-xs text-slate-400">Delayed</p>
            <p class="mt-1 text-2xl font-semibold text-red-300">{{ s.delayedCount }}</p>
          </div>
        </div>

        <section class="mt-8 rounded-2xl border border-white/10 bg-white/5 p-5">
          <div class="mb-4 flex items-center justify-between">
            <h2 class="font-medium">Orders by hour of day</h2>
            @if (peakLabel(); as p) {
              <span class="text-xs text-slate-400">Peak: {{ p }}</span>
            }
          </div>
          <div class="flex h-40 items-end gap-1">
            @for (h of s.ordersByHour; track h.hour) {
              <div class="flex h-full flex-1 flex-col justify-end" [title]="label(h.hour) + ': ' + h.count + ' orders'">
                <div class="w-full rounded-t bg-indigo-400/80" [style.height.%]="(h.count / maxCount()) * 100"
                     [class.min-h-px]="h.count > 0"></div>
              </div>
            }
          </div>
          <div class="mt-1 flex gap-1 text-[10px] text-slate-500">
            @for (h of s.ordersByHour; track h.hour) {
              <span class="flex-1 text-center">{{ h.hour % 3 === 0 ? label(h.hour) : '' }}</span>
            }
          </div>
        </section>

        <section class="mt-6 rounded-2xl border border-white/10 bg-white/5 p-5">
          <h2 class="mb-1 font-medium">Delay risk</h2>
          <p class="mb-4 text-xs text-slate-400">
            Active orders open for 30+ minutes are "At risk", 45+ minutes are "Delayed" (target: {{ sla }} min).
          </p>
          @if (s.delayedOrders.length === 0) {
            <p class="text-sm text-slate-400">No orders at risk right now.</p>
          } @else {
            <div class="space-y-3">
              @for (d of s.delayedOrders; track d.id) {
                <div>
                  <div class="mb-1 flex items-center justify-between text-sm">
                    <span>#{{ d.id }} {{ d.restaurantName }}
                      <span class="ml-1 rounded-full px-2 py-0.5 text-xs" [class]="badge(d.status)">{{ d.status }}</span>
                    </span>
                    <span [class]="d.risk === 'Delayed' ? 'text-red-300' : 'text-amber-300'">
                      {{ d.elapsedMinutes }} min, {{ d.risk === 'Delayed' ? 'Delayed' : 'At risk' }}
                    </span>
                  </div>
                  <div class="h-2 rounded bg-white/10">
                    <div class="h-2 rounded" [style.width.%]="pct(d.elapsedMinutes)"
                         [class]="d.risk === 'Delayed' ? 'bg-red-400' : 'bg-amber-400'"></div>
                  </div>
                </div>
              }
            </div>
          }
        </section>
      } @else if (!error()) {
        <p class="text-slate-400">Loading dashboard...</p>
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