import { Component } from '@angular/core';
import { AbstractControl, AsyncValidatorFn, FormBuilder, Validators } from '@angular/forms';
import { AccountService } from '../account.service';
import { Route, Router } from '@angular/router';
import { debounceTime, finalize, map, switchMap, take } from 'rxjs';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  errors: String[] | null = null;


  constructor(private fb: FormBuilder, private accountService: AccountService, private router: Router){}

  complexPassword = "(?=^.{6,10}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+}{\":;'?/>.<,])(?!.*\\s).*$"

  registerForm = this.fb.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email], [this.validateEmailNotTaken()]],
      password: ['', [Validators.required, Validators.pattern(this.complexPassword)]],
  })

  onSubmit(){
    this.accountService.register(this.registerForm.value).subscribe({
    next: () => this.router.navigateByUrl('/shop'),
    error: error => this.errors = error.errors
    })
  }

  validateEmailNotTaken(): AsyncValidatorFn{
    return (control: AbstractControl) => {
      return control.valueChanges.pipe(
        debounceTime(1000), //asa face un singur API call dupa 1 secunda in loc sa gfaca dupa fiecare modificare a fiecarei litere
        take(1),
        switchMap(() => {
          return this.accountService.checkEmailExists(control.value).pipe(
        map(result => result ? {emailExists: true} : null),
        finalize(() => control.markAsTouched())
      )
        })
      )
      
    }
  }
}
