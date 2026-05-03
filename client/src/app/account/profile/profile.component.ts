import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../account.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  addressForm!: FormGroup;
  passwordForm!: FormGroup; // Am adăugat formularul pentru parolă
  activeTab: 'address' | 'password' = 'address'; // Variabila care controlează ce tab vedem

  constructor(
    private fb: FormBuilder, 
    private accountService: AccountService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.createAddressForm();
    this.createPasswordForm(); // Inițializăm și formularul de parolă la încărcare
    this.getAddress();
  }

  // --- LOGICA PENTRU ADRESĂ (ce aveai deja, neatins) ---
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

  // --- LOGICA NOUĂ PENTRU PAROLĂ ---
  createPasswordForm() {
    // Același Regex strict pe care l-am setat și în C#
    const passwordRegex = "(?=^.{6,10}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&amp;*()_+}{&quot;:;'?/&gt;.&lt;,])(?!.*\\s).*$";
    
    this.passwordForm = this.fb.group({
      oldPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.pattern(passwordRegex)]],
      confirmPassword: ['', Validators.required]
    });
  }

  onSubmitPassword() {
    const values = this.passwordForm.value;

    // Validare simplă: ne asigurăm că Bob a scris aceeași parolă de două ori
    if (values.newPassword !== values.confirmPassword) {
      this.toastr.error('Parolele noi nu coincid!');
      return; // Oprim execuția aici dacă nu coincid
    }

    // Trimitem către serviciu (vezi că am adăugat logica în serviciu în pasul anterior)
    this.accountService.changePassword(values).subscribe({
      next: (res: any) => {
        this.toastr.success(res?.message || 'Parola a fost schimbată cu succes!');
        this.passwordForm.reset(); // Curățăm câmpurile ca să nu mai fie vizibile parolele
      },
      error: error => {
        console.log(error);
        this.toastr.error('Eroare. Posibil ca parola curentă să fie incorectă.');
      }
    });
  }
}