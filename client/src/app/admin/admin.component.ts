import { Component, OnInit } from '@angular/core';
import { AdminService } from './admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.scss']
})
export class AdminComponent implements OnInit {
  users: any[] = [];

  constructor(private adminService: AdminService, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getUsers();
  }

  getUsers() {
    this.adminService.getUsersWithRoles().subscribe({
      next: users => this.users = users,
      error: (error: any) => console.log(error)
    });
  }

  approveUser(id: string) {
    this.adminService.approveUser(id).subscribe({
      next: () => {
        // Găsim user-ul în listă și îi schimbăm statusul vizual, fără să dăm refresh la pagină
        const userIndex = this.users.findIndex(u => u.id === id);
        if (userIndex !== -1) {
          this.users[userIndex].isVerified = true;
        }
        this.toastr.success('Producător aprobat cu succes!');
      },
      error: (error: any) => {
        console.log(error);
        this.toastr.error('Eroare la aprobarea utilizatorului.');
      }
    });
  }
}