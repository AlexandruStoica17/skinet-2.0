import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getUsersWithRoles() {
    return this.http.get<any[]>(this.baseUrl + 'admin/users-with-roles');
  }

  approveUser(id: string) {
    return this.http.post(this.baseUrl + 'admin/approve-user/' + id, {});
  }
}