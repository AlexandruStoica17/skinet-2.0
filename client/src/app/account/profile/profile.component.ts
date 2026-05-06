import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../account.service';
import { Observable } from 'rxjs';
import { User } from 'src/app/shared/models/user';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  addressForm!: FormGroup;
  passwordForm!: FormGroup; 
  activeTab: 'address' | 'password' | 'verification' = 'address'; // Am adăugat 'verification'
  
  user$: Observable<User | null>; // Pentru a afișa tab-ul doar producătorilor
  selectedFile: File | null = null; // Fișierul selectat de utilizator

  constructor(
    private fb: FormBuilder, 
    private accountService: AccountService,
    private toastr: ToastrService
  ) { 
    this.user$ = this.accountService.currentUser$;
  }

  ngOnInit(): void {
    this.createAddressForm();
    this.createPasswordForm(); 
    this.getAddress();
  }

  // --- LOGICA PENTRU ADRESĂ ---
  createAddressForm() {
    this.addressForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      street: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      zipCode: ['', Validators.required]
    });
  }

  getAddress() {
    this.accountService.getUserAddress().subscribe({
      next: address => {
        if (address) {
          this.addressForm.patchValue(address);
        }
      },
      error: error => console.log(error)
    });
  }

  onSubmitAddress() {
    this.accountService.updateUserAddress(this.addressForm.value).subscribe({
      next: address => {
        this.addressForm.patchValue(address);
        this.addressForm.markAsPristine();
        this.toastr.success('Adresa a fost actualizată cu succes!');
      },
      error: error => console.log(error)
    });
  }

  // --- LOGICA PENTRU PAROLĂ ---
  createPasswordForm() {
    const passwordRegex = "(?=^.{6,10}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&amp;*()_+}{&quot;:;'?/&gt;.&lt;,])(?!.*\\s).*$";
    
    this.passwordForm = this.fb.group({
      oldPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.pattern(passwordRegex)]],
      confirmPassword: ['', Validators.required]
    });
  }

  onSubmitPassword() {
    const values = this.passwordForm.value;

    if (values.newPassword !== values.confirmPassword) {
      this.toastr.error('Parolele noi nu coincid!');
      return; 
    }

    this.accountService.changePassword(values).subscribe({
      next: (res: any) => {
        this.toastr.success(res?.message || 'Parola a fost schimbată cu succes!');
        this.passwordForm.reset(); 
      },
      error: error => {
        console.log(error);
        this.toastr.error('Eroare. Posibil ca parola curentă să fie incorectă.');
      }
    });
  }

  // --- LOGICA NOUĂ PENTRU VERIFICARE CONT ---
  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
  }

  onUploadDocument() {
    if (!this.selectedFile) return;

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.accountService.uploadVerificationDocument(formData).subscribe({
      next: () => {
        this.toastr.success('Document trimis! Așteaptă aprobarea administratorului.');
        this.selectedFile = null;
      },
      error: error => {
        console.log(error);
        this.toastr.error('Eroare la încărcarea fișierului.');
      }
    });
  }
}