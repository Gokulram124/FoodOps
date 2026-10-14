import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../core/admin.service';
import { AdminRestaurant, OwnerSummary } from '../core/models';

@Component({
  selector: 'app-admin-restaurants',
  imports: [ReactiveFormsModule, CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1100px">
      <h1 class="h3 fw-semibold mb-4">Restaurants</h1>

      <form class="card card-body mb-4" [formGroup]="form" (ngSubmit)="create()">
        <h2 class="h6 fw-medium mb-3">Add restaurant</h2>
        @if (error()) {
          <div class="alert alert-danger py-2 small">{{ error() }}</div>
        }
        @if (success()) {
          <div class="alert alert-success py-2 small">{{ success() }}</div>
        }
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label small">Restaurant name</label>
            <input class="form-control" formControlName="name" />
          </div>
          <div class="col-md-3">
            <label class="form-label small">City</label>
            <input class="form-control" formControlName="city" />
          </div>
          <div class="col-md-5">
            <label class="form-label small">Owner</label>
            <select class="form-select" formControlName="ownerId">
              <option value="">Select owner...</option>
              @for (o of owners(); track o.id) {
                <option [value]="o.id">{{ o.fullName }} ({{ o.email }})</option>
              }
            </select>
          </div>
        </div>
        <div class="mt-3">
          <button class="btn btn-primary" [disabled]="saving()">Add restaurant</button>
        </div>
      </form>

      <div class="card">
        <div class="table-responsive">
          <table class="table mb-0 align-middle">
            <thead>
              <tr>
                <th>Restaurant</th>
                <th>Owner</th>
                <th>Status</th>
                <th class="text-end">Orders today</th>
                <th class="text-end">Revenue today</th>
                <th class="text-end">Active now</th>
              </tr>
            </thead>
            <tbody>
              @for (r of restaurants(); track r.id) {
                <tr>
                  <td>
                    {{ r.name }}
                    <div class="small text-body-secondary">{{ r.city }}</div>
                  </td>
                  <td>{{ r.ownerName }}</td>
                  <td>
                    <span class="badge rounded-pill" [class]="r.isOpen ? 'text-bg-success' : 'text-bg-secondary'">
                      {{ r.isOpen ? 'Open' : 'Closed' }}
                    </span>
                  </td>
                  <td class="text-end">{{ r.ordersToday }}</td>
                  <td class="text-end">{{ r.revenueToday | currency: 'INR' : 'symbol' : '1.0-0' }}</td>
                  <td class="text-end">{{ r.activeNow }}</td>
                </tr>
              } @empty {
                <tr><td colspan="6" class="text-body-secondary small">No restaurants yet.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class AdminRestaurantsComponent {
  private svc = inject(AdminService);
  private fb = inject(FormBuilder);

  restaurants = signal<AdminRestaurant[]>([]);
  owners = signal<OwnerSummary[]>([]);
  error = signal('');
  success = signal('');
  saving = signal(false);

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    city: ['', Validators.required],
    ownerId: ['', Validators.required],
  });

  constructor() {
    this.svc.owners().subscribe({
      next: o => this.owners.set(o),
      error: () => this.error.set('Could not load owners.'),
    });
    this.load();
  }

  load() {
    this.svc.restaurants().subscribe({
      next: r => this.restaurants.set(r),
      error: () => this.error.set('Could not load restaurants.'),
    });
  }

  create() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Fill the name, city and choose an owner.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    this.success.set('');

    this.svc.createRestaurant(this.form.getRawValue()).subscribe({
      next: r => {
        this.success.set(`Restaurant "${r.name}" added.`);
        this.form.reset();
        this.saving.set(false);
        this.load();
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(e.error?.detail ?? e.error?.message ?? e.error?.title ?? 'Could not add restaurant.');
        this.saving.set(false);
      },
    });
  }
}