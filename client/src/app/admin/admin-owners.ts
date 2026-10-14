import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../core/admin.service';
import { OwnerSummary } from '../core/models';

@Component({
  selector: 'app-admin-owners',
  imports: [ReactiveFormsModule],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
      <h1 class="h3 fw-semibold mb-4">Restaurant owners</h1>

      <form class="card card-body mb-4" [formGroup]="form" (ngSubmit)="create()">
        <h2 class="h6 fw-medium mb-3">Create owner</h2>
        @if (error()) {
          <div class="alert alert-danger py-2 small">{{ error() }}</div>
        }
        @if (success()) {
          <div class="alert alert-success py-2 small">{{ success() }}</div>
        }
        <div class="row g-3">
          <div class="col-md-4">
            <label class="form-label small">Full name</label>
            <input class="form-control" formControlName="fullName" />
          </div>
          <div class="col-md-4">
            <label class="form-label small">Email</label>
            <input class="form-control" type="email" formControlName="email" />
          </div>
          <div class="col-md-4">
            <label class="form-label small">Temporary password</label>
            <input class="form-control" type="password" formControlName="password" autocomplete="new-password" />
          </div>
        </div>
        <div class="mt-3">
          <button class="btn btn-primary" [disabled]="saving()">Create owner</button>
        </div>
      </form>

      <div class="card">
        <table class="table mb-0 align-middle">
          <thead>
            <tr><th>Name</th><th>Email</th><th class="text-end">Restaurants</th></tr>
          </thead>
          <tbody>
            @for (o of owners(); track o.id) {
              <tr>
                <td>{{ o.fullName }}</td>
                <td>{{ o.email }}</td>
                <td class="text-end">{{ o.restaurantCount }}</td>
              </tr>
            } @empty {
              <tr><td colspan="3" class="text-body-secondary small">No owners yet.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class AdminOwnersComponent {
  private svc = inject(AdminService);
  private fb = inject(FormBuilder);

  owners = signal<OwnerSummary[]>([]);
  error = signal('');
  success = signal('');
  saving = signal(false);

  form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor() {
    this.load();
  }

  load() {
    this.svc.owners().subscribe({
      next: o => this.owners.set(o),
      error: () => this.error.set('Could not load owners.'),
    });
  }

  create() {
     if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Fill all fields. Use a valid email and a password of at least 8 characters.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    this.success.set('');

    this.svc.createOwner(this.form.getRawValue()).subscribe({
      next: o => {
        this.success.set(`Owner ${o.email} created.`);
        this.form.reset();
        this.saving.set(false);
        this.load();
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(e.error?.detail ?? e.error?.message ?? e.error?.title ?? 'Could not create owner.');
        this.saving.set(false);
      },
    });
  }
}