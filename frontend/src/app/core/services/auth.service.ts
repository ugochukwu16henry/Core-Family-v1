import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, tap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RefreshTokenRequest, RegisterRequest } from '../models/auth.model';
import { UserSummary } from '../models/user.model';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly currentUserSignal = signal<UserSummary | null>(this.restoreUser());

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => !!this.currentUserSignal() && !!this.tokenStorage.getAccessToken());

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, payload).pipe(
      tap((response) => this.persistSession(response))
    );
  }

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, payload).pipe(
      tap((response) => this.persistSession(response))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.tokenStorage.getRefreshToken();

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }

    const payload: RefreshTokenRequest = { refreshToken };

    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, payload).pipe(
      tap((response) => this.persistSession(response)),
      catchError((error) => {
        this.logout(false);
        return throwError(() => error);
      })
    );
  }

  logout(callApi = true): void {
    if (callApi) {
      this.http.post<void>(`${environment.apiUrl}/auth/logout`, {}).subscribe({
        next: () => undefined,
        error: () => undefined
      });
    }

    this.tokenStorage.clear();
    this.currentUserSignal.set(null);
  }

  getAccessToken(): string | null {
    return this.tokenStorage.getAccessToken();
  }

  getProfile(): Observable<UserSummary> {
    return this.http.get<UserSummary>(`${environment.apiUrl}/users/me`).pipe(
      tap((user) => this.currentUserSignal.set(user))
    );
  }

  private persistSession(response: AuthResponse): void {
    this.tokenStorage.saveSession(response);
    this.currentUserSignal.set(response.user);
  }

  private restoreUser(): UserSummary | null {
    const rawUser = this.tokenStorage.getStoredUser();

    if (!rawUser) {
      return null;
    }

    try {
      return JSON.parse(rawUser) as UserSummary;
    } catch {
      this.tokenStorage.clear();
      return null;
    }
  }
}