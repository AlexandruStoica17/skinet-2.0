import { HttpClient, HttpHeaderResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Address, User } from '../shared/models/user';
import { BehaviorSubject, map, of, ReplaySubject } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  uploadVerificationDocument(formData: FormData) {
    // Adaugă 'return' aici!
    return this.http.post(this.baseUrl + 'account/upload-document', formData);
  }
  baseUrl = environment.apiUrl;
  private currentUserSource = new ReplaySubject<User | null>(1);
  currentUser$ = this.currentUserSource.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

 loadCurrentUser(token: string | null) {
    if (token === null) {
      this.currentUserSource.next(null);
      return of(null);
    }
    let headers = new HttpHeaders();
    headers = headers.set('Authorization', `Bearer ${token}`);

    return this.http.get<User>(this.baseUrl + 'account', { headers }).pipe(
      map(user => {
        if (user) {
          // FOLOSIM FUNCȚIA NOASTRĂ AICI!
          this.setCurrentUser(user); 
          return user;
        } else {
          return null;
        }
      })
    )
  }

  login(values: any) {
    return this.http.post<User>(this.baseUrl + 'account/login', values).pipe(
      map(user => {
        // FOLOSIM FUNCȚIA NOASTRĂ AICI!
        this.setCurrentUser(user);
      })
    )
  }

  register(values: any) {
    return this.http.post<User>(this.baseUrl + 'account/register', values).pipe(
      map(user => {
        // FOLOSIM FUNCȚIA NOASTRĂ AICI!
        this.setCurrentUser(user);
      })
    )
  }

  logout() {
    localStorage.removeItem('token');
    this.currentUserSource.next(null);
    this.router.navigateByUrl('/');
  }

  checkEmailExists(email: string) {
    return this.http.get(this.baseUrl + 'account/emailexists?email=' + email);
  }

  getUserAddress(){
    return this.http.get<Address>(this.baseUrl + 'account/address');
  }
  
  updateUserAddress(address: Address){
    return this.http.put(this.baseUrl + 'account/address', address);
  }

  changePassword(values: any) {
    // Trimitem datele către noul endpoint din C#
    return this.http.post(this.baseUrl + 'account/change-password', values);
  }
  setCurrentUser(user: User | null) {
    if (user) {
      // 1. Decodificăm token-ul
      const decodedToken = this.getDecodedToken(user.token);
      
      // 2. Extragem rolul (C# folosește uneori o cheie lungă pentru roluri, așa că le verificăm pe ambele)
      user.role = decodedToken.role || decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      
      // 3. Extragem statusul de verificare (în token e salvat ca string 'true' / 'false')
      user.isVerified = decodedToken.isVerified === 'true';

      // 4. Salvăm în localStorage și actualizăm aplicația
      localStorage.setItem('token', user.token);
      this.currentUserSource.next(user);
    } else {
      localStorage.removeItem('token');
      this.currentUserSource.next(null);
    }
  }

  getDecodedToken(token: string) {
    return JSON.parse(atob(token.split('.')[1]));
  }
  
}
