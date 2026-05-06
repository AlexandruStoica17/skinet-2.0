import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ShopService } from 'src/app/shop/shop.service';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';

@Component({
  selector: 'app-add-product',
  templateUrl: './add-product.component.html',
  styleUrls: ['./add-product.component.scss']
})
export class AddProductComponent implements OnInit {
  productForm!: FormGroup;
  brands: any[] = [];
  types: any[] = [];
  selectedFile: File | null = null;
  imagePreview: string | ArrayBuffer | null = null;

  constructor(
    private fb: FormBuilder,
    private shopService: ShopService,
    private toastr: ToastrService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.createForm();
    this.getBrands();
    this.getTypes();
  }

  createForm() {
    this.productForm = this.fb.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
      price: ['', [Validators.required, Validators.min(0.01)]],
      productTypeId: ['', Validators.required],
      productBrandId: ['', Validators.required]
    });
  }

  getBrands() {
    this.shopService.getBrands().subscribe({
      next: response => this.brands = response,
      error: (error: any) => console.log(error)
    });
  }

  getTypes() {
    this.shopService.getTypes().subscribe({
      next: response => this.types = response,
      error: (error: any) => console.log(error)
    });
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      
      // Creăm un preview al imaginii selectate
      const reader = new FileReader();
      reader.onload = e => this.imagePreview = reader.result;
      reader.readAsDataURL(file);
    }
  }

  onSubmit() {
    if (!this.selectedFile) {
      this.toastr.error('Te rugăm să adaugi o poză pentru produs.');
      return;
    }

    // Împachetăm datele pentru a putea trimite fișierul fizic (poza)
    const formData = new FormData();
    formData.append('name', this.productForm.get('name')?.value);
    formData.append('description', this.productForm.get('description')?.value);
    formData.append('price', this.productForm.get('price')?.value);
    formData.append('productTypeId', this.productForm.get('productTypeId')?.value);
    formData.append('productBrandId', this.productForm.get('productBrandId')?.value);
    formData.append('picture', this.selectedFile);

    // Apelăm endpoint-ul tău nou din C# creat anterior
    this.shopService.addProduct(formData).subscribe({
      next: () => {
        this.toastr.success('Produsul a fost adăugat cu succes!');
        this.router.navigate(['/shop']); // Îl trimitem în magazin să își vadă creația
      },
      error: (error: any) => {
        console.log(error);
        this.toastr.error('A apărut o eroare la salvarea produsului.');
      }
    });
  }
}