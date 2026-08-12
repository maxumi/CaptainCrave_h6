import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { AuthState, Role } from '../../shared/models/user';
import { environment } from '../../../environments/environment';

interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  role: Role;
  address?: string;
  latitude?: number | null;
  longitude?: number | null;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface AuthResponse {
  id: number;
  name: string;
  email: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
  role: string;
  token: string;
}


@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authStateKey = 'authState';
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/Auth`;

  readonly user = signal<AuthState | null>(this.readAuthState());

  login(data: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.url}/login`, data).pipe(
      tap({
        next: (response) => {
          this.setAuthState({
            userId: response.id,
            name: response.name,
            email: response.email,
            address: response.address ?? '',
            latitude: response.latitude ?? null,
            longitude: response.longitude ?? null,
            role: response.role,
            token: response.token,
          });
        },
      })
    );
  }

  register(data: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.url}/register`, data).pipe(
      tap({
        next: (response) => {
          this.setAuthState({
            userId: response.id,
            name: response.name,
            email: response.email,
            address: response.address ?? '',
            latitude: response.latitude ?? null,
            longitude: response.longitude ?? null,
            role: response.role,
            token: response.token,
          }); 
        }
      })
    );
  }

  logout(): void {
    this.setAuthState(null);
  }

  getToken(): string | null {
    return this.user()?.token ?? null;
  }

  private readAuthState(): AuthState | null {
    const storedState = localStorage.getItem(this.authStateKey);

    if (!storedState) {
      return null;
    }

    try {
      const parsedState = JSON.parse(storedState) as Partial<AuthState>;

      if (!parsedState.token) {
        localStorage.removeItem(this.authStateKey);
        return null;
      }

      return parsedState as AuthState;
    } catch {
      localStorage.removeItem(this.authStateKey);
      return null;
    }
  }

  private setAuthState(authState: AuthState | null): void {
    if (authState) {
      localStorage.setItem(this.authStateKey, JSON.stringify(authState));
    } else {
      localStorage.removeItem(this.authStateKey);
    }

    this.user.set(authState);
  }

  updateCurrentUserProfile(profile: {
    name: string;
    address: string;
    latitude: number | null;
    longitude: number | null;
  }): void {
    const current = this.user();
    if (!current) {
      return;
    }

    this.setAuthState({
      ...current,
      name: profile.name,
      address: profile.address,
      latitude: profile.latitude,
      longitude: profile.longitude,
    });
  }

  isLoggedIn(): boolean {
    return !!this.user()?.token;
  }

  hasRole(roles: Role[]): boolean {
    const currentRole = this.user()?.role as Role | undefined;

    if (!currentRole) {
      return false;
    }

    return roles.includes(currentRole);
  }
}