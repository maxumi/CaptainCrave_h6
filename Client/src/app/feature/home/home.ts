import { Component, inject, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { TranslocoModule } from '@jsverse/transloco';

import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../shared/models/user';
import { RestaurantApiService } from '../../shared/restaurant-api.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, TranslocoModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly restaurantApiService = inject(RestaurantApiService);

  ngOnInit(): void {
    const user = this.authService.user();

    if (!user || user.role !== Role.Restaurant) {
      return;
    }

    this.restaurantApiService.getMyRestaurant().pipe(
      map(() => false),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return of(true);
        }

        return of(false);
      })
    ).subscribe((shouldRedirectToCreate) => {
      if (shouldRedirectToCreate) {
        void this.router.navigate(['/restaurant-create']);
      }
    });
  }
}