import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from 'src/app/account/account.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {

  constructor(private accountService: AccountService, private router: Router, private toastr: ToastrService) {}

 canActivate(): Observable<boolean> {
    return this.accountService.currentUser$.pipe(
      map(user => {
        // SCHIMBĂM AICI: folosim .includes('Admin') 
        if (user && user.role?.includes('Admin')) {
          return true;
        }
        
        this.toastr.error('Nu ai acces la această secțiune!');
        this.router.navigateByUrl('/');
        return false;
      })
    );
  }
}