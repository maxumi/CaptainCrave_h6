import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";

@Injectable({
  providedIn: 'root',
})
export class ProfileService {
    http = inject(HttpClient);
    // update name and address
    updateProfile(name: string, address: string): void {
        // comment out
        // use httpclient to send updated profile to backend
        this.http.post('/api/profile/update', { name, address }).subscribe({
            next: (response) => {
                console.log('Profile updated successfully', response);
            },
            error: (error) => {
                console.error('Error updating profile', error);
            }
        });
    }
}