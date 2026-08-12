import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserProfileDto {
  id: number;
  name: string;
  email: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
  role: string;
}

export interface UpdateUserProfileRequest {
  name: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
}

@Injectable({
  providedIn: 'root',
})
export class UserApiService {
  private readonly http = inject(HttpClient);
  private readonly usersUrl = `${environment.apiUrl}/Users`;

  getMyProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.usersUrl}/me`);
  }

  updateMyProfile(payload: UpdateUserProfileRequest): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(`${this.usersUrl}/me`, payload);
  }
}
