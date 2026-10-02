import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Role } from '../core/models';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="flex min-h-[80vh] items-center justify-center px-4">
      <form [formGroup]="form" (ngSubmit)="submit()"
            class="w-full max-w-md space-y-4 rounded-2xl border border-white/10 bg-white/5 p-8 backdrop-blur">
        <h1 class="text-2xl font-semibold">Create account</h1>

        @if (error()) {
          <div class="rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
        }

        <div>
          <label class="mb-1 block text-sm text-slate-300">Full name</label>
          <input formControlName="fullName"
                 class="w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
        </div>

        <div>
          <label class="mb-1 block text-sm text-slate-300">Email</label>
          <input type="email" formControlName="email"
                 class="w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
        </div>

        <div>
          <label class="mb-1 block text-sm text-slate-300">Password</label>
          <input type="password" formControlName="password"
                 class="w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
          <p class="mt-1 text-xs text-slate-500">Min 6 chars, with upper, lower and a digit</p>
        </div>

        <div>
          <label class="mb-1 block text-sm text-slate-300">I am a</label>
          <select formControlName="role"
                  class="w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400">
            <option value="Customer">Customer</option>
            <option value="RestaurantOwner">Restaurant Owner</option>
            <option value="DeliveryPartner">Delivery Partner</option>
          </select>
        </div>

        <button type="submit" [disabled]="loading()"
                class="w-full rounded-lg bg-indigo-500 py-2 font-medium hover:bg-indigo-400 disabled:opacity-50">
          {{ loading() ? 'Creating...' : 'Register' }}
        </button>

        <p class="text-center text-sm text-slate-400">
          Already have an account? <a routerLink="/login" class="text-indigo-300 hover:underline">Login</a>
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
