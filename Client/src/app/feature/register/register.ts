import { Component, inject, signal } from '@angular/core';
import {
  form,
  required,
  email,
  minLength,
  validate,
  FormField,
  FormRoot,
} from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { getAuthErrorMessage } from '../../shared/getAuthErrorMessage';
import { firstValueFrom } from 'rxjs';
import { Role } from '../../shared/models/user';



interface RegisterFormData {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: Role;
}

@Component({
  selector: 'app-register',
  imports: [TranslocoModule, RouterLink, FormField,  FormRoot],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  authService = inject(AuthService);
  private readonly translocoService = inject(TranslocoService);

  readonly isSubmitting = signal(false);
  readonly registerError = signal<string | null>(null);
  private readonly router = inject(Router);
  readonly Role = Role;

  registerModel = signal<RegisterFormData>({
    name: '',
    email: '',
    password: '',
    confirmPassword: '',
    role: Role.Customer,
  });
registerForm = form(
  this.registerModel,
  (schemaPath) => {
    required(schemaPath.name, {
      message: this.translocoService.translate('register.validation.nameRequired'),
    });

    required(schemaPath.email, {
      message: this.translocoService.translate('register.validation.emailRequired'),
    });
    email(schemaPath.email, {
      message: this.translocoService.translate('register.validation.emailInvalid'),
    });

    required(schemaPath.password, {
      message: this.translocoService.translate('register.validation.passwordRequired'),
    });
    minLength(schemaPath.password, 8, {
      message: this.translocoService.translate('register.validation.passwordMinLength'),
    });

    required(schemaPath.confirmPassword, {
      message: this.translocoService.translate('register.validation.confirmPasswordRequired'),
    });
    required(schemaPath.role, {
      message: this.translocoService.translate('register.validation.roleRequired'),
    });

    validate(schemaPath.confirmPassword, ({ value, valueOf }) => {
      if (value() !== valueOf(schemaPath.password)) {
        return {
          kind: 'passwordMismatch',
          message: this.translocoService.translate('register.validation.passwordMismatch'),
        };
      }

      return null;
    });
  },
  {
    submission: {
      action: async () => {
        this.registerError.set(null);
        this.isSubmitting.set(true);

        try {
          await firstValueFrom(
            this.authService.register({
              name: this.registerForm.name().value(),
              email: this.registerForm.email().value(),
              password: this.registerForm.password().value(),
              role: this.registerForm.role().value(),
            }),
          );

          const role = this.registerForm.role().value();

          if (role === Role.Restaurant) {
            await this.router.navigate(['/restaurant-create']);
          } else {
            await this.router.navigate(['/']);
          }

          return null;
        } catch (error) {
          const message = getAuthErrorMessage(
            error,
            this.translocoService.translate('register.error.createFailed'),
          );

          this.registerError.set(message);

          return {
            kind: 'serverError',
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