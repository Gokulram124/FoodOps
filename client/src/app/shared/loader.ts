import { Component, computed, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, timer } from 'rxjs';

const FOODS = ['🍕', '🍔', '🍜', '🍛', '🌮', '🍩'];

@Component({
  selector: 'app-loader',
  template: `
    <div class="food-loader" [class.full]="fullPage()" role="status" aria-live="polite">
      <div class="ring">
        <div class="spinner-border text-primary" aria-hidden="true"></div>
        <span class="emoji" aria-hidden="true">{{ emoji() }}</span>
      </div>
      <div class="msg">{{ message() }}</div>
      @if (slow()) {
        <div class="hint">The kitchen is warming up (free hosting). Almost ready...</div>
      }
    </div>
  `,
  styles: [`
    .food-loader {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 3rem 1rem;
      text-align: center;
    }
    .food-loader.full { min-height: 60vh; }
    .ring {
      position: relative;
      width: 76px;
      height: 76px;
      display: grid;
      place-items: center;
    }
    .ring .spinner-border {
      position: absolute;
      inset: 0;
      --bs-spinner-width: 76px;
      --bs-spinner-height: 76px;
      --bs-spinner-border-width: 0.22rem;
    }
    .emoji {
      font-size: 1.9rem;
      line-height: 1;
      animation: hop 0.9s ease-in-out infinite;
    }
    @keyframes hop {
      0%, 100% { transform: translateY(0) scale(1); }
      50% { transform: translateY(-6px) scale(1.12); }
    }
    .msg {
      margin-top: 1rem;
      font-size: 0.95rem;
      color: var(--bs-secondary-color);
    }
    .hint {
      margin-top: 0.35rem;
      font-size: 0.8rem;
      color: var(--bs-secondary-color);
      opacity: 0.85;
    }
    @media (prefers-reduced-motion: reduce) {
      .emoji { animation: none; }
    }
  `],
})
export class LoaderComponent {
  message = input('Cooking things up...');
  fullPage = input(false);

  private index = signal(0);
  emoji = computed(() => FOODS[this.index() % FOODS.length]);
  slow = signal(false);

  constructor() {
    interval(900).pipe(takeUntilDestroyed()).subscribe(() => this.index.update(i => i + 1));
    timer(6000).pipe(takeUntilDestroyed()).subscribe(() => this.slow.set(true));
  }
}