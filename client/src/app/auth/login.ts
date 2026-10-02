import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="flex min-h-[80vh] items-center justify-center px-4">
      <form [formGroup]="form" (ngSubmit)="submit()"
            class="w-full max-w-md space-y-4 rounded-2xl border border-white/10 bg-white/5 p-8 backdrop-blur">
        <h1 class="text-2xl font-semibold">Welcome back</h1>
        <p class="text-sm text-slate-400">Login to FoodOps</p>

        @if (error()) {
          <div class="rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-300">{{ error() }}</div>
        }

        <div>
          <label class="mb-1 block text-sm text-slate-300">Email</label>
          <input type="email" formControlName="email"
                 class="w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
          @if (form.controls.email.touched && form.controls.email.invalid) {
            <p class="mt-1 text-xs text-red-300">Enter a valid email</p>
          }
        </div>

        <div>
          <label class="mb-1 block text-sm text-slate-300">Password</label>
          <input type="password" formControlName="password"
                 class="w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-2 outline-none focus:border-indigo-400" />
          @if (form.controls.password.touched && form.controls.password.invalid) {
            <p class="mt-1 text-xs text-red-300">Password is required</p>
          }
        </div>

        <button type="submit" [disabled]="loading()"
                class="w-full rounded-lg bg-indigo-500 py-2 font-medium hover:bg-indigo-400 disabled:opacity-50">
          {{ loading() ? 'Signing in...' : 'Login' }}
        </button>

        <p class="text-center text-sm text-slate-400">
          New here? <a routerLink="/register" class="text-indigo-300 hover:underline">Create an account</a>
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
