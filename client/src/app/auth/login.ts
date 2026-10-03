import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="d-flex justify-content-center align-items-center px-3" style="min-height: 80vh">
      <form [formGroup]="form" (ngSubmit)="submit()" class="card card-body p-4 w-100 shadow-sm" style="max-width: 448px">
        <h1 class="h3 fw-semibold">Welcome back</h1>
        <p class="text-body-secondary small">Login to FoodOps</p>

        @if (error()) {
          <div class="alert alert-danger py-2 small">{{ error() }}</div>
        }

        <div class="mb-3">
          <label class="form-label small">Email</label>
          <input type="email" formControlName="email" class="form-control"
                 [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid" />
          <div class="invalid-feedback">Enter a valid email</div>
        </div>

        <div class="mb-3">
          <label class="form-label small">Password</label>
          <input type="password" formControlName="password" class="form-control"
                 [class.is-invalid]="form.controls.password.touched && form.controls.password.invalid" />
          <div class="invalid-feedback">Password is required</div>
        </div>

        <button type="submit" [disabled]="loading()" class="btn btn-primary w-100">
          {{ loading() ? 'Signing in...' : 'Login' }}
        </button>

        <p class="text-center text-body-secondary small mt-3 mb-0">
          New here? <a routerLink="/register">Create an account</a>
        </p>
      </form>
    </div>
  `,
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal('');

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set('');

    this.auth.login(this.form.getRawValue()).subscribe({
      next: res => this.router.navigateByUrl(this.auth.homeFor(res.role)),
      error: err => {
        this.error.set(err.error?.message ?? 'Could not reach the server. Is the API running?');
        this.loading.set(false);
      },
    });
  }
}
