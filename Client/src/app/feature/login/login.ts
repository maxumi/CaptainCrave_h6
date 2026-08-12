import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { getAuthErrorMessage } from '../../shared/getAuthErrorMessage';
import {
  form,
  required,
  email,
  FormField,
  FormRoot,
} from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
interface LoginFormData {
  email: string;
  password: string;
}

@Component({
  selector: 'app-login',
  imports: [TranslocoModule, RouterLink, FormField, FormRoot],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  authService = inject(AuthService);
  private readonly translocoService = inject(TranslocoService);
  readonly isSubmitting = signal(false);
  readonly loginError = signal<string | null>(null);

  // The router is used to navigate to the home page after a successful login
  private readonly router = inject(Router);

  loginModel = signal<LoginFormData>({
    email: '',
    password: '',
  });

  // The forms validation schema and given requirements
  loginForm = form(
    this.loginModel,
    (schemaPath) => {
      required(schemaPath.email, {
        message: this.translocoService.translate('login.validation.emailRequired'),
      });
      email(schemaPath.email, {
        message: this.translocoService.translate('login.validation.emailInvalid'),
      });

      required(schemaPath.password, {
        message: this.translocoService.translate('login.validation.passwordRequired'),
      });
    },
    {
      submission: {
        action: async () => {
          // When the form is consired submitted, this code is run below
          this.loginError.set(null);
          this.isSubmitting.set(true);

          try {
            // since function is async, it needs to convert observables to promise and await them
            await firstValueFrom(
              this.authService.login({
                email: this.loginForm.email().value(),
                password: this.loginForm.password().value(),
              }),
            );

            await this.router.navigate(['/']);

            return null;
          } catch (error) {
            const message = getAuthErrorMessage(
              error,
              this.translocoService.translate('login.error.signInFailed'),
            );

            this.loginError.set(message);

            return {
              kind: 'serverError' as const,
              message,
            };
          } finally {
            this.isSubmitting.set(false);
          }
        },
      },
    },
  );
}