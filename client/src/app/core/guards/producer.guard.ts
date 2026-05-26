import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccountService } from 'src/app/account/account.service';

@Injectable({
  providedIn: 'root'
})
export class ProducerGuard implements CanActivate {

  constructor(
    private accountService: AccountService, 
    private router: Router, 
    private toastr: ToastrService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> {
      
    // Ne uităm la utilizatorul conectat în acest moment
    return this.accountService.currentUser$.pipe(
      map(user => {
        if (user) {
          // Verificăm dacă are rolul de producător
          if (user.role?.includes('CosmeticsProducer') || user.role?.includes('IngredientsProducer')) {
            return true; // Îi permitem accesul pe pagină
          }
        }
        
        // Dacă ajunge aici, înseamnă că e client normal sau nu e logat deloc
        this.toastr.error('Access denied! This section is reserved for producers.');
        this.router.navigate(['/shop']); // Îl trimitem înapoi în magazin
        return false; // Îi blocăm accesul
      })
    );
  }
}
