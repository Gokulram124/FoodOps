import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../core/admin.service';
import { Coupon, CreateCouponRequest, CustomerOption, DiscountType } from '../core/models';
import { toDate } from '../core/utc';

// <input type="datetime-local"> wants "YYYY-MM-DDTHH:mm" in the browser's local time
function localInput(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`;
}

@Component({
  selector: 'app-admin-coupons',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1100px">
      <h1 class="h3 fw-semibold mb-4">Coupons</h1>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }
      @if (success()) {
        <div class="alert alert-success py-2 small">{{ success() }}</div>
      }

      <form [formGroup]="form" (ngSubmit)="create()" class="card card-body mb-4">
        <h2 class="h6 fw-medium mb-3">New coupon</h2>
        <div class="row g-3">
          <div class="col-md-3">
            <label class="form-label small" for="code">Code</label>
            <input id="code" formControlName="code" class="form-control text-uppercase" maxlength="30"
                   [class.is-invalid]="bad('code')" placeholder="WELCOME20">
            @if (bad('code')) { <div class="invalid-feedback">4-30 letters, digits, - or _</div> }
          </div>
          <div class="col-md-3">
            <label class="form-label small" for="type">Type</label>
            <select id="type" formControlName="type" class="form-select"
                    (change)="isPercent.set($any($event.target).value === 'Percent')">
              <option value="Percent">Percent (%)</option>
              <option value="Flat">Flat amount (INR)</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label small" for="value">{{ isPercent() ? 'Percent off' : 'Amount off (INR)' }}</label>
            <input id="value" type="number" formControlName="value" class="form-control" min="1"
                   [class.is-invalid]="bad('value')">
            @if (bad('value')) { <div class="invalid-feedback">Enter a value above 0</div> }
          </div>
          @if (isPercent()) {
            <div class="col-md-3">
              <label class="form-label small" for="max">Max discount (INR, optional)</label>
              <input id="max" type="number" formControlName="maxDiscount" class="form-control" min="1">
            </div>
          }
          <div class="col-md-3">
            <label class="form-label small" for="min">Minimum order (INR)</label>
            <input id="min" type="number" formControlName="minOrderAmount" class="form-control" min="0">
          </div>
          <div class="col-md-3">
            <label class="form-label small" for="from">Valid from</label>
            <input id="from" type="datetime-local" formControlName="validFrom" class="form-control"
                   [class.is-invalid]="bad('validFrom')">
          </div>
          <div class="col-md-3">
            <label class="form-label small" for="to">Valid until</label>
            <input id="to" type="datetime-local" formControlName="validTo" class="form-control"
                   [class.is-invalid]="bad('validTo')">
          </div>
          <div class="col-md-3">
            <label class="form-label small" for="limit">Total uses (optional)</label>
            <input id="limit" type="number" formControlName="usageLimit" class="form-control" min="1">
          </div>
          <div class="col-md-6">
            <label class="form-label small" for="user">For</label>
            <select id="user" formControlName="assignedUserId" class="form-select">
              <option value="">Anyone (each customer can use it once)</option>
              @for (u of customers(); track u.id) {
                <option [value]="u.id">{{ u.fullName }} ({{ u.email }})</option>
              }
            </select>
          </div>
          <div class="col-md-6 d-flex align-items-end">
            <button type="submit" [disabled]="busy()" class="btn btn-primary">
              {{ busy() ? 'Creating...' : 'Create coupon' }}
            </button>
          </div>
        </div>
      </form>

      <div class="card">
        <div class="table-responsive">
          <table class="table mb-0 align-middle small">
            <thead>
              <tr>
                <th>Code</th><th>Discount</th><th>Min order</th><th>Valid</th>
                <th>For</th><th class="text-end">Used</th><th>Status</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (c of coupons(); track c.id) {
                <tr>
                  <td class="fw-medium">{{ c.code }}</td>
                  <td>
                    @if (c.type === 'Percent') {
                      {{ c.value }}% off
                      @if (c.maxDiscount) { <span class="text-body-secondary">(up to {{ c.maxDiscount | currency: 'INR' : 'symbol' : '1.0-0' }})</span> }
                    } @else {
                      {{ c.value | currency: 'INR' : 'symbol' : '1.0-0' }} off
                    }
                  </td>
                  <td>{{ c.minOrderAmount | currency: 'INR' : 'symbol' : '1.0-0' }}</td>
                  <td>
                    {{ time(c.validFromUtc) | date: 'dd MMM yy, HH:mm' }}<br>
                    <span class="text-body-secondary">to {{ time(c.validToUtc) | date: 'dd MMM yy, HH:mm' }}</span>
                  </td>
                  <td>{{ c.assignedUserName ?? 'Anyone' }}</td>
                  <td class="text-end">{{ c.usedCount }}{{ c.usageLimit ? ' / ' + c.usageLimit : '' }}</td>
                  <td><span class="badge rounded-pill" [class]="badge(c.status)">{{ c.status }}</span></td>
                  <td class="text-end">
                    <button (click)="toggle(c)" [disabled]="busy()" class="btn btn-sm btn-outline-secondary">
                      {{ c.isActive ? 'Disable' : 'Enable' }}
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="8" class="text-body-secondary">No coupons yet. Create the first one above.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class AdminCouponsComponent {
  private svc = inject(AdminService);
  private fb = inject(FormBuilder);

  readonly time = toDate;

  coupons = signal<Coupon[]>([]);
  customers = signal<CustomerOption[]>([]);
  isPercent = signal(true);
  busy = signal(false);
  error = signal('');
  success = signal('');

  form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(30), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    type: ['Percent' as DiscountType],
    value: [10, [Validators.required, Validators.min(1)]],
    maxDiscount: [null as number | null],
    minOrderAmount: [0, [Validators.min(0)]],
    validFrom: [localInput(new Date()), Validators.required],
    validTo: [localInput(new Date(Date.now() + 30 * 24 * 3600 * 1000)), Validators.required],
    assignedUserId: [''],
    usageLimit: [null as number | null],
  });

  constructor() {
    this.svc.coupons().subscribe({
      next: list => this.coupons.set(list),
      error: () => this.error.set('Could not load coupons.'),
    });
    this.svc.customers().subscribe(list => this.customers.set(list));
  }

  bad(name: 'code' | 'value' | 'validFrom' | 'validTo'): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  badge(status: string): string {
    switch (status) {
      case 'Active': return 'bg-success-subtle text-success-emphasis';
      case 'Scheduled': return 'bg-info-subtle text-info-emphasis';
      case 'Disabled': return 'bg-warning-subtle text-warning-emphasis';
      default: return 'bg-secondary-subtle text-secondary-emphasis';   // Expired, Used up
    }
  }

  create() {
    this.error.set('');
    this.success.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Please fix the highlighted fields.');
      return;
    }

    const v = this.form.getRawValue();
    const body: CreateCouponRequest = {
      code: v.code.trim(),
      type: v.type,
      value: Number(v.value),
      maxDiscount: v.type === 'Percent' && v.maxDiscount ? Number(v.maxDiscount) : null,
      minOrderAmount: Number(v.minOrderAmount) || 0,
      // the picker gives local time; the API stores UTC
      validFromUtc: new Date(v.validFrom).toISOString(),
      validToUtc: new Date(v.validTo).toISOString(),
      assignedUserId: v.assignedUserId || null,
      usageLimit: v.usageLimit ? Number(v.usageLimit) : null,
    };

    this.busy.set(true);
    this.svc.createCoupon(body).subscribe({
      next: c => {
        this.coupons.update(list => [c, ...list]);
        this.success.set(`Coupon ${c.code} created.`);
        this.busy.set(false);
        this.form.reset({
          code: '', type: 'Percent', value: 10, maxDiscount: null, minOrderAmount: 0,
          validFrom: localInput(new Date()),
          validTo: localInput(new Date(Date.now() + 30 * 24 * 3600 * 1000)),
          assignedUserId: '', usageLimit: null,
        });
        this.isPercent.set(true);
      },
      error: e => {
        this.busy.set(false);
        this.error.set(e.error?.detail ?? e.error?.message ?? e.error?.title ?? 'Could not create the coupon.');
      },
    });
  }

  toggle(c: Coupon) {
    this.busy.set(true);
    this.error.set('');
    this.success.set('');
    this.svc.setCouponActive(c.id, !c.isActive).subscribe({
      next: saved => {
        this.coupons.update(list => list.map(x => (x.id === saved.id ? saved : x)));
        this.busy.set(false);
      },
      error: e => {
        this.busy.set(false);
        this.error.set(e.error?.detail ?? 'Could not update the coupon.');
      },
    });
  }
}