import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ShopService } from 'src/app/shop/shop.service';

@Component({
  selector: 'app-edit-product',
  templateUrl: './edit-product.component.html',
  styleUrls: ['./edit-product.component.scss']
})
export class EditProductComponent implements OnInit {
  productForm!: FormGroup;
  brands: any[] = [];
  types: any[] = [];
  productId!: number;
  existingImageUrl: string = '';
  selectedFile: File | null = null;
  imagePreview: string | ArrayBuffer | null = null;

  constructor(
    private fb: FormBuilder,
    private shopService: ShopService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute // Pentru a lua ID-ul din URL
  ) { }

  ngOnInit(): void {
    this.createForm();
    
    // 1. Luăm ID-ul din link
    this.productId = +this.route.snapshot.paramMap.get('id')!;

    // 2. Cerem Brandurile
    this.shopService.getBrands().subscribe(brands => {
      this.brands = brands;
      
      // 3. După ce avem brandurile, cerem Tipurile
      this.shopService.getTypes().subscribe(types => {
        this.types = types;
        
        // 4. După ce avem și tipurile gata în dropdown, încărcăm produsul ca să se bifeze corect
        if (this.productId) {
          this.loadProduct();
        }
      });
    });
  }

  createForm() {
    this.productForm = this.fb.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
      price: ['', [Validators.required, Validators.min(1)]],
      productTypeId: ['', Validators.required],
      productBrandId: ['', Validators.required]
    });
  }

  loadProduct() {
    this.shopService.getProduct(this.productId).subscribe({
      next: product => {
        // Pre-completăm formularul cu datele primite
        this.productForm.patchValue({
          name: product.name,
          description: product.description,
          price: product.price,
          productTypeId: product.productTypeId,
          productBrandId: product.productBrandId
        });
        this.existingImageUrl = product.pictureUrl;
      },
      error: error => console.log(error)
    });
  }

  getBrands() {
    this.shopService.getBrands().subscribe(response => this.brands = response);
  }

  getTypes() {
    this.shopService.getTypes().subscribe(response => this.types = response);
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = e => this.imagePreview = reader.result;
      reader.readAsDataURL(file);
    }
  }

  onSubmit() {
    const formData = new FormData();
    formData.append('name', this.productForm.get('name')?.value);
    formData.append('description', this.productForm.get('description')?.value);
    formData.append('price', this.productForm.get('price')?.value);
    formData.append('productTypeId', this.productForm.get('productTypeId')?.value);
    formData.append('productBrandId', this.productForm.get('productBrandId')?.value);
    
    // Adăugăm poza doar dacă utilizatorul a ales una nouă
    if (this.selectedFile) {
      formData.append('picture', this.selectedFile);
    }

    this.shopService.editProduct(this.productId, formData).subscribe({
      next: () => {
        this.toastr.success('Produsul a fost actualizat!');
        this.router.navigate(['/my-products']);
      },
      error: error => this.toastr.error('Eroare la actualizare')
    });
  }
}