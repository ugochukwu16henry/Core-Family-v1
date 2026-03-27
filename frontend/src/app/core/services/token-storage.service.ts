import { Injectable } from '@angular/core';

import { AuthResponse } from '../models/auth.model';

const ACCESS_TOKEN_KEY = 'core-family.access-token';
const REFRESH_TOKEN_KEY = 'core-family.refresh-token';
const AUTH_USER_KEY = 'core-family.auth-user';
const EXPIRES_AT_KEY = 'core-family.expires-at';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  saveSession(response: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(response.user));
    localStorage.setItem(EXPIRES_AT_KEY, response.expiresAt);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  getStoredUser(): string | null {
    return localStorage.getItem(AUTH_USER_KEY);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USER_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
  }
}