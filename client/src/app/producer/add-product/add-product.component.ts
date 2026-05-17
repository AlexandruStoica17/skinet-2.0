import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ShopService } from 'src/app/shop/shop.service';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { AccountService } from 'src/app/account/account.service';
import { take } from 'rxjs';

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

  // Is the current user an admin? (controls brand visibility)
  isAdmin = false;

  // Filter options
  skinTypeOptions = ['Oily', 'Dry', 'Combination', 'Sensitive', 'Normal', 'All Skin Types'];
  usageOptions    = ['Face', 'Eyes', 'Lips', 'Hands', 'Body', 'Hair', 'Neck & Décolletage'];
  benefitOptions  = ['Hydration', 'Anti-aging', 'SPF Protection', 'Brightening',
                     'Pore Cleansing', 'Firming', 'Soothing', 'Exfoliating',
                     'Nourishing', 'Mattifying'];
  formulaOptions  = ['Cream', 'Serum', 'Lotion', 'Emulsion', 'Oil', 'Gel',
                     'Foam', 'Toner', 'Mask', 'Scrub', 'Liquid', 'Balm', 'Powder'];

  // MULTISELECT: selected values per filter
  selectedSkinTypes: string[] = [];
  selectedUsages: string[]    = [];
  selectedBenefits: string[]  = [];
  selectedFormulas: string[]  = [];

  constructor(
    private fb: FormBuilder,
    private shopService: ShopService,
    private toastr: ToastrService,
    private router: Router,
    private accountService: AccountService  // to check if admin
  ) { }

  ngOnInit(): void {
    this.createForm();
    this.getTypes();

    // Check if logged-in user is Admin
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      if (user?.role?.includes('Admin')) {
        this.isAdmin = true;
        this.getBrands(); // only load brands for admin
      }
    });
  }

  createForm() {
    this.productForm = this.fb.group({
      name:          ['', Validators.required],
      description:   ['', Validators.required],
      price:         ['', [Validators.required, Validators.min(0.01)]],
      productTypeId: ['', Validators.required],
      // Brand only required for admin
      productBrandId: ['']
    });
  }

  getBrands() {
    this.shopService.getBrands().subscribe({
      next: response => this.brands = response,
      error: (err: any) => console.log(err)
    });
  }

  getTypes() {
    this.shopService.getTypes().subscribe({
      next: response => this.types = response,
      error: (err: any) => console.log(err)
    });
  }

  // MULTISELECT toggle helpers
  toggleOption(arr: string[], value: string): string[] {
    const idx = arr.indexOf(value);
    if (idx > -1) { arr.splice(idx, 1); } else { arr.push(value); }
    return [...arr];
  }

  isChecked(arr: string[], value: string): boolean {
    return arr.includes(value);
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      const reader = new FileReader();
      reader.onload = () => this.imagePreview = reader.result;
      reader.readAsDataURL(file);
    }
  }

  onSubmit() {
    if (!this.selectedFile) {
      this.toastr.error('Please add a product image.');
      return;
    }

    const formData = new FormData();
    formData.append('name',          this.productForm.get('name')?.value);
    formData.append('description',   this.productForm.get('description')?.value);
    formData.append('price',         this.productForm.get('price')?.value);
    formData.append('productTypeId', this.productForm.get('productTypeId')?.value);
    formData.append('picture',       this.selectedFile);

    // Brand: only send if admin selected one
    if (this.isAdmin && this.productForm.get('productBrandId')?.value) {
      formData.append('productBrandId', this.productForm.get('productBrandId')?.value);
    } else {
      formData.append('productBrandId', '0'); // backend will auto-assign seller's brand
    }

    // MULTISELECT: join arrays to comma-separated strings
    formData.append('skinType',  this.selectedSkinTypes.join(','));
    formData.append('usage',     this.selectedUsages.join(','));
    formData.append('benefits',  this.selectedBenefits.join(','));
    formData.append('formula',   this.selectedFormulas.join(','));

    this.shopService.addProduct(formData).subscribe({
      next: () => {
        this.toastr.success('Product published successfully!');
        this.router.navigate(['/shop']);
      },
      error: (err: any) => {
        console.log(err);
        this.toastr.error('Error saving the product. Please try again.');
      }
    });
  }
}