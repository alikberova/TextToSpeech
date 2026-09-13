import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_URL, GUEST_JWT_EXPIRES_KEY, GUEST_JWT_KEY } from '../../../constants/tokens';
import { AUTH_GUEST } from '../../http/endpoints';

interface GuestTokenResponse {
  accessToken: string;
  expiresAtUtc: string;
}

interface TokenState {
  token: string;
  expiresAtUtc: string;
};

@Injectable({ providedIn: 'root' })
export class GuestTokenService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  private state: TokenState | null = null;
  private refreshPromise: Promise<string> | null = null;

  getToken(): string | null {
    const state = this.getState();
    return state?.token ?? null;
  }

  isTokenValid(bufferSeconds = 30): boolean {
    const state = this.getState();
    if (!state) {
      return false;
    }

    const expiresMs = Date.parse(state.expiresAtUtc);
    if (Number.isNaN(expiresMs)) {
      return false;
    }

    const nowMs = Date.now();
    return nowMs + bufferSeconds * 1000 < expiresMs;
  }

  clearToken(): void {
    this.state = null;
    sessionStorage.removeItem(GUEST_JWT_KEY);
    sessionStorage.removeItem(GUEST_JWT_EXPIRES_KEY);
  }

  async ensureValidToken(persistInSession = false): Promise<string> {
    if (this.isTokenValid()) {
      return this.getState()!.token;
    }

    return this.refreshToken(persistInSession);
  }

  async refreshToken(persistInSession = false): Promise<string> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    this.refreshPromise = (async () => {
      const url = `${this.apiUrl}${AUTH_GUEST}`;
      const res = await firstValueFrom(this.http.post<GuestTokenResponse>(url, {}));

      this.setState(
        { token: res.accessToken, expiresAtUtc: res.expiresAtUtc },
        persistInSession
      );

      return res.accessToken;
    })();

    try {
      return await this.refreshPromise;
    } finally {
      this.refreshPromise = null;
    }
  }

  private getState(): TokenState | null {
    if (this.state) {
      return this.state;
    }

    const token = sessionStorage.getItem(GUEST_JWT_KEY);
    const expiresAtUtc = sessionStorage.getItem(GUEST_JWT_EXPIRES_KEY);

    if (token && expiresAtUtc) {
      this.state = { token, expiresAtUtc };
      return this.state;
    }

    return null;
  }

  private setState(state: TokenState, persistInSession: boolean): void {
    this.state = state;

    if (persistInSession) {
      sessionStorage.setItem(GUEST_JWT_KEY, state.token);
      sessionStorage.setItem(GUEST_JWT_EXPIRES_KEY, state.expiresAtUtc);
      return;
    }

    sessionStorage.removeItem(GUEST_JWT_KEY);
    sessionStorage.removeItem(GUEST_JWT_EXPIRES_KEY);
  }
}
