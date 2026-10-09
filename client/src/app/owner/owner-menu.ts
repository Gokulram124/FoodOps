import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { imageSrc } from '../core/config';
import { MenuService } from '../core/menu.service';
import { MenuItem, Restaurant } from '../core/models';
import { RestaurantService } from '../core/restaurant.service';

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_BYTES = 2 * 1024 * 1024;

@Component({
  selector: 'app-owner-menu',
  imports: [ReactiveFormsModule, CurrencyPipe],
  template: `
    <div class="mx-auto px-3 px-md-4 py-4" style="max-width: 1024px">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <h1 class="h3 fw-semibold m-0">Menu management</h1>
        @if (restaurants().length > 1) {
          <select class="form-select w-auto" (change)="selectRestaurant($any($event.target).value)">
            @for (r of restaurants(); track r.id) {
              <option [value]="r.id" [selected]="r.id === restaurantId()">{{ r.name }}</option>
            }
          </select>
        } @else if (restaurants().length === 1) {
          <span class="text-body-secondary">{{ restaurants()[0].name }}</span>
        }
      </div>

      @if (error()) {
        <div class="alert alert-danger py-2 small">{{ error() }}</div>
      }
      @if (success()) {
        <div class="alert alert-success py-2 small">{{ success() }}</div>
      }

      @if (loading()) {
        <p class="text-body-secondary">Loading...</p>
      } @else if (restaurantId() === null) {
        <p class="text-body-secondary">No restaurant is linked to your account.</p>
      } @else {
        <div class="row g-4">
          <div class="col-lg-7">
            <div class="card">
              <div class="list-group list-group-flush">
                @for (m of items(); track m.id) {
                  <div class="list-group-item">
                    <div class="d-flex align-items-center gap-3">
                      @if (img(m.imageUrl); as src) {
                        <img [src]="src" [alt]="m.name" width="56" height="56" class="rounded object-fit-cover flex-shrink-0">
                      } @else {
                        <div class="rounded bg-body-secondary flex-shrink-0" style="width: 56px; height: 56px"></div>
                      }
                      <div class="flex-fill">
                        <div>
                          {{ m.name }}
                          @if (!m.isAvailable) {
                            <span class="badge rounded-pill bg-secondary-subtle text-secondary-emphasis ms-1">Hidden</span>
                          }
                        </div>
                        <div class="small text-body-secondary">
                          {{ m.category }} · {{ m.price | currency: 'INR' : 'symbol' : '1.0-0' }}
                        </div>
                      </div>
                      <div class="d-flex gap-2 flex-shrink-0">
                        <button (click)="edit(m)" class="btn btn-sm btn-outline-secondary">Edit</button>
                        @if (confirmId() === m.id) {
                          <button (click)="remove(m)" [disabled]="busy()" class="btn btn-sm btn-danger">Sure?</button>
                          <button (click)="confirmId.set(null)" class="btn btn-sm btn-outline-secondary">No</button>
                        } @else {
                          <button (click)="confirmId.set(m.id)" class="btn btn-sm btn-outline-danger">Delete</button>
                        }
                      </div>
                    </div>
                  </div>
                } @empty {
                  <div class="list-group-item text-body-secondary">No menu items yet. Add your first one.</div>
                }
              </div>
            </div>
          </div>

          <div class="col-lg-5">
            <form [formGroup]="form" (ngSubmit)="save()" class="card card-body">
              <h2 class="h6 fw-medium mb-3">{{ editingId() ? 'Edit item' : 'Add item' }}</h2>

              <div class="mb-3">
                <label class="form-label small" for="name">Name</label>
                <input id="name" formControlName="name" class="form-control" maxlength="100">
                @if (show('name')) { <div class="text-danger small mt-1">Name is required.</div> }
              </div>

              <div class="row g-2 mb-3">
                <div class="col-6">
                  <label class="form-label small" for="price">Price (INR)</label>
                  <input id="price" type="number" formControlName="price" class="form-control" min="1" step="1">
                  @if (show('price')) { <div class="text-danger small mt-1">Enter a price above 0.</div> }
                </div>
                <div class="col-6">
                  <label class="form-label small" for="category">Category</label>
                  <input id="category" formControlName="category" class="form-control" maxlength="50">
                  @if (show('category')) { <div class="text-danger small mt-1">Category is required.</div> }
                </div>
              </div>

              <div class="form-check mb-3">
                <input id="avail" type="checkbox" formControlName="isAvailable" class="form-check-input">
                <label class="form-check-label small" for="avail">Available to customers</label>
              </div>

              <div class="mb-3">
                <label class="form-label small" for="file">Photo (jpg, png, webp, max 2 MB)</label>
                <input id="file" type="file" accept="image/jpeg,image/png,image/webp" (change)="onFile($event)" class="form-control">
                @if (fileError()) { <div class="text-danger small mt-1">{{ fileError() }}</div> }
                @if (preview(); as p) {
                  <img [src]="p" alt="Preview" width="96" height="96" class="rounded object-fit-cover mt-2">
                }
              </div>

              <div class="d-flex gap-2">
                <button type="submit" [disabled]="busy()" class="btn btn-primary">
                  {{ busy() ? 'Saving...' : (editingId() ? 'Save changes' : 'Add item') }}
                </button>
                @if (editingId()) {
                  <button type="button" (click)="reset()" class="btn btn-outline-secondary">Cancel</button>
                }
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
})
export class OwnerMenuComponent {
  private fb = inject(FormBuilder);
  private menu = inject(MenuService);
  private restaurantService = inject(RestaurantService);

  readonly img = imageSrc;

  restaurants = signal<Restaurant[]>([]);
  restaurantId = signal<number | null>(null);
  items = signal<MenuItem[]>([]);
  loading = signal(true);
  busy = signal(false);
  error = signal('');
  success = signal('');
  editingId = signal<number | null>(null);
  confirmId = signal<number | null>(null);
  fileError = signal('');
  preview = signal<string | null>(null);

  private file: File | null = null;

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    price: [0, [Validators.required, Validators.min(1)]],
    category: ['', [Validators.required, Validators.maxLength(50)]],
    isAvailable: [true],
  });

  constructor() {
    this.restaurantService.mine().subscribe({
      next: list => {
        this.restaurants.set(list);
        if (list.length > 0) {
          this.restaurantId.set(list[0].id);
          this.loadItems();
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.error.set('Could not load your restaurants. Check that the API is running.');
        this.loading.set(false);
      },
    });
  }

  show(name: 'name' | 'price' | 'category'): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  selectRestaurant(value: string) {
    this.restaurantId.set(Number(value));
    this.reset();
    this.loadItems();
  }

  private loadItems() {
    const id = this.restaurantId();
    if (id === null) return;
    this.loading.set(true);
    this.menu.getByRestaurant(id).subscribe({
      next: list => {
        this.items.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load the menu.');
        this.loading.set(false);
      },
    });
  }

  onFile(event: Event) {
    const input = event.target as HTMLInputElement;
    const f = input.files?.[0] ?? null;
    this.fileError.set('');
    this.file = null;
    this.preview.set(null);
    if (!f) return;

    if (!ALLOWED_TYPES.includes(f.type)) {
      this.fileError.set('Only jpg, png or webp images are allowed.');
      input.value = '';
      return;
    }
    if (f.size > MAX_BYTES) {
      this.fileError.set('Image must be smaller than 2 MB.');
      input.value = '';
      return;
    }
    this.file = f;
    this.preview.set(URL.createObjectURL(f));
  }

  edit(m: MenuItem) {
    this.editingId.set(m.id);
    this.confirmId.set(null);
    this.error.set('');
    this.success.set('');
    this.form.setValue({
      name: m.name,
      price: m.price,
      category: m.category,
      isAvailable: m.isAvailable,
    });
    this.clearFile();
  }

  reset() {
    this.editingId.set(null);
    this.form.reset({ name: '', price: 0, category: '', isAvailable: true });
    this.clearFile();
  }

  private clearFile() {
    this.file = null;
    this.fileError.set('');
    const p = this.preview();
    if (p) URL.revokeObjectURL(p);
    this.preview.set(null);
    const el = document.getElementById('file') as HTMLInputElement | null;
    if (el) el.value = '';
  }

  save() {
    const restaurantId = this.restaurantId();
    if (restaurantId === null) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.success.set('');

    const input = { restaurantId, ...this.form.getRawValue() };
    const id = this.editingId();
    const request$ = id ? this.menu.update(id, input) : this.menu.create(input);

    request$.subscribe({
      next: saved => {
        const itemId = id ?? saved?.id;
        if (this.file && itemId) {
          this.menu.uploadImage(itemId, this.file).subscribe({
            next: () => this.done(id ? 'Item updated.' : 'Item added.'),
            error: err => {
              this.error.set(err.error?.message ?? 'Item saved, but the image upload failed.');
              this.afterSave();
            },
          });
        } else {
          this.done(id ? 'Item updated.' : 'Item added.');
        }
      },
      error: err => {
        this.busy.set(false);
        this.error.set(err.error?.message ?? 'Could not save this item.');
      },
    });
  }

  private done(message: string) {
    this.success.set(message);
    this.afterSave();
  }

  private afterSave() {
    this.busy.set(false);
    this.reset();
    this.loadItems();
  }

  remove(m: MenuItem) {
    this.busy.set(true);
    this.error.set('');
    this.success.set('');
    this.menu.remove(m.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.confirmId.set(null);
        // API hides items that were ordered before, so reload to show the real state
        this.success.set('Item removed (or hidden, if customers ordered it before).');
        this.loadItems();
      },
      error: err => {
        this.busy.set(false);
        this.confirmId.set(null);
        this.error.set(err.error?.message ?? 'Could not delete this item.');
      },
    });
  }
}
