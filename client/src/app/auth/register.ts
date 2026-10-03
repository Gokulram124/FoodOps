import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Role } from '../core/models';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="d-flex justify-content-center align-items-center px-3 py-4" style="min-height: 80vh">
      <form [formGroup]="form" (ngSubmit)="submit()" class="card card-body p-4 w-100 shadow-sm" style="max-width: 448px">
        <h1 class="h3 fw-semibold mb-3">Create account</h1>

        @if (error()) {
          <div class="alert alert-danger py-2 small">{{ error() }}</div>
        }

        <div class="mb-3">
          <label class="form-label small">Full name</label>
          <input formControlName="fullName" class="form-control" />
        </div>

        <div class="mb-3">
          <label class="form-label small">Email</label>
          <input type="email" formControlName="email" class="form-control" />
        </div>

        <div class="mb-3">
          <label class="form-label small">Password</label>
          <input type="password" formControlName="password" class="form-control" />
          <div class="form-text">Min 6 chars, with upper, lower and a digit</div>
        </div>

        <div class="mb-3">
          <label class="form-label small">I am a</label>
          <select formControlName="role" class="form-select">
            <option value="Customer">Customer</option>
            <option value="RestaurantOwner">Restaurant Owner</option>
            <option value="DeliveryPartner">Delivery Partner</option>
          </select>
        </div>

        <button type="submit" [disabled]="loading()" class="btn btn-primary w-100">
          {{ loading() ? 'Creating...' : 'Register' }}
        </button>

        <p class="text-center text-body-secondary small mt-3 mb-0">
          Already have an account? <a routerLink="/login">Login</a>
        </p>
      </form>
    </div>
  `,
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal('');

  form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['Customer' as Role, Validators.required],
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set('');

    this.auth.register(this.form.getRawValue()).subscribe({
      next: res => this.router.navigateByUrl(this.auth.homeFor(res.role)),
      error: err => {
        this.error.set(err.error?.message ?? 'Registration failed. Is the API running?');
        this.loading.set(false);
      },
    });
  }
}
