import { Component } from '@angular/core';
import { AbstractControl, AsyncValidatorFn, FormBuilder, Validators } from '@angular/forms';
import { AccountService } from '../account.service';
import { Router } from '@angular/router';
import { debounceTime, finalize, map, switchMap, take } from 'rxjs';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  errors: string[] | null = null;
  
  // Lista de roluri pentru dropdown
  roles = [
    { value: 'Buyer', display: 'Buyer' },
    { value: 'CosmeticsProducer', display: 'Cosmetics Producer' },
    { value: 'IngredientsProducer', display: 'Ingredients Producer' },
    { value: 'Blogger', display: 'Blogger' }
  ];

  complexPassword = "(?=^.{6,10}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+}{\":;'?/>.<,])(?!.*\\s).*$"

  registerForm = this.fb.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email], [this.validateEmailNotTaken()]],
    password: ['', [Validators.required, Validators.pattern(this.complexPassword)]],
    role: ['Buyer', Validators.required],
    companyName: [''] // Îl lăsăm gol la început
  })

  constructor(private fb: FormBuilder, private accountService: AccountService, private router: Router) {
    // Adăugăm un listener pentru a valida Numele Firmei doar dacă e producător
    this.registerForm.get('role')?.valueChanges.subscribe(role => {
      const companyControl = this.registerForm.get('companyName');
      if (role && role.includes('Producer')) {
        companyControl?.setValidators([Validators.required]);
      } else {
        companyControl?.clearValidators();
      }
      companyControl?.updateValueAndValidity();
    });
  }

  onSubmit() {
    this.accountService.register(this.registerForm.value).subscribe({
      next: () => this.router.navigateByUrl('/shop'),
      error: error => this.errors = error.errors
    })
  }

  validateEmailNotTaken(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      return control.valueChanges.pipe(
        debounceTime(1000),
        take(1),
        switchMap(() => {
          return this.accountService.checkEmailExists(control.value).pipe(
            map(result => result ? { emailExists: true } : null),
            finalize(() => control.markAsTouched())
          )
        })
      )
    }
  }
}
