import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function passwordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string;
    if (!value) return null;

    const errors: ValidationErrors = {};

    if (value.length < 6) errors['passwordMinLength'] = true;
    if (value.length > 30) errors['passwordMaxLength'] = true;
    if (!/[A-Z]/.test(value)) errors['passwordUppercase'] = true;
    if (!/[a-z]/.test(value)) errors['passwordLowercase'] = true;
    if (!/\d/.test(value)) errors['passwordNumber'] = true;
    if (!/[^A-Za-z0-9\s]/.test(value)) errors['passwordSpecial'] = true;
    if (/\s/.test(value)) errors['passwordWhitespace'] = true;

    return Object.keys(errors).length ? errors : null;
  };
}
