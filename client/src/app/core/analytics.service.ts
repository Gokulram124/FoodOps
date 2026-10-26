import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from './config';
import { AnalyticsDashboard, LeaderboardRow, RevenueTrendPoint } from './analytics.models';

// UTC without the trailing "Z" (ASP.NET Core would convert a "Z" date to server local time)
const iso = (d: Date) => d.toISOString().slice(0, 19);

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private http = inject(HttpClient);

  private range(from: Date, to: Date): HttpParams {
    return new HttpParams().set('from', iso(from)).set('to', iso(to));
  }

  dashboard(from: Date, to: Date): Observable<AnalyticsDashboard> {
    return this.http.get<AnalyticsDashboard>(`${API_URL}/v1/analytics/dashboard`, {
      params: this.range(from, to),
    });
  }

  leaderboard(from: Date, to: Date): Observable<LeaderboardRow[]> {
    return this.http.get<LeaderboardRow[]>(`${API_URL}/v1/analytics/leaderboard`, {
      params: this.range(from, to),
    });
  }

  revenueTrend(from: Date, to: Date, granularity: 'day' | 'week' | 'month' = 'day'): Observable<RevenueTrendPoint[]> {
    const params = this.range(from, to)
      .set('granularity', granularity)
      .set('tzOffsetMinutes', -new Date().getTimezoneOffset());
    return this.http.get<RevenueTrendPoint[]>(`${API_URL}/v1/analytics/revenue-trend`, { params });
  }
}