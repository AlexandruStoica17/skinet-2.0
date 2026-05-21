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

  selectedFiles: File[] = [];
  imagePreviews: string[] = [];

  // NOU: pozele actuale cu id, url, isMain, displayOrder
  existingPhotos: any[] = [];

  constructor(
    private fb: FormBuilder,
    private shopService: ShopService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.createForm();

    this.productId = +this.route.snapshot.paramMap.get('id')!;

    this.shopService.getBrands().subscribe({
      next: brands => {
        this.brands = brands;

        this.shopService.getTypes().subscribe({
          next: types => {
            this.types = types;

            if (this.productId) {
              this.loadProduct();
            }
          },
          error: error => console.log(error)
        });
      },
      error: error => console.log(error)
    });
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

  loadProduct() {
    this.shopService.getProduct(this.productId).subscribe({
      next: product => {
        this.productForm.patchValue({
          name: product.name,
          description: product.description,
          price: product.price,
          productTypeId: product.productTypeId,
          productBrandId: product.productBrandId
        });

        // NOU: afișăm pozele în ordinea lor
        if (product.photos && product.photos.length > 0) {
          this.existingPhotos = product.photos
            .sort((a: any, b: any) => a.displayOrder - b.displayOrder);
        } else if (product.pictureUrl) {
          this.existingPhotos = [
            {
              id: 0,
              url: product.pictureUrl,
              isMain: true,
              displayOrder: 1
            }
          ];
        } else {
          this.existingPhotos = [];
        }
      },
      error: error => console.log(error)
    });
  }

  onFilesSelected(event: any) {
    const files = event.target.files;

    if (!files || files.length === 0) return;

    this.selectedFiles = Array.from(files);
    this.imagePreviews = [];

    this.selectedFiles.forEach(file => {
      const reader = new FileReader();

      reader.onload = () => {
        this.imagePreviews.push(reader.result as string);
      };

      reader.readAsDataURL(file);
    });
  }

  removeSelectedImage(index: number) {
    this.selectedFiles.splice(index, 1);
    this.imagePreviews.splice(index, 1);
  }

  setMainPhoto(photoId: number) {
    if (photoId === 0) {
      this.toastr.info('Salvează mai întâi produsul ca poza să fie înregistrată în galerie.');
      return;
    }

    this.shopService.setMainPhoto(this.productId, photoId).subscribe({
      next: () => {
        this.toastr.success('Poza principală a fost actualizată.');
        this.loadProduct();
      },
      error: error => {
        console.log(error);
        this.toastr.error('Nu s-a putut seta poza principală.');
      }
    });
  }

  deletePhoto(photoId: number) {
    if (photoId === 0) {
      this.toastr.info('Această poză veche trebuie salvată întâi în galerie.');
      return;
    }

    if (!confirm('Sigur vrei să ștergi această poză?')) return;

    this.shopService.deleteProductPhoto(this.productId, photoId).subscribe({
      next: () => {
        this.toastr.success('Poza a fost ștearsă.');
        this.loadProduct();
      },
      error: error => {
        console.log(error);
        this.toastr.error('Nu s-a putut șterge poza.');
      }
    });
  }

 movePhoto(photoId: number, direction: 'up' | 'down') {
  if (photoId === 0) {
    this.toastr.info('Această poză veche trebuie salvată întâi în galerie.');
    return;
  }

  this.shopService.moveProductPhoto(this.productId, photoId, direction).subscribe({
    next: () => {
      this.toastr.success('Ordinea pozelor a fost actualizată.');
      this.loadProduct();
    },
    error: error => {
      console.log(error);
      this.toastr.error('Nu s-a putut schimba ordinea pozelor.');
    }
  });
}

  onSubmit() {
    if (this.productForm.invalid) {
      this.toastr.error('Completează toate câmpurile obligatorii.');
      return;
    }

    const formData = new FormData();

    formData.append('name', this.productForm.get('name')?.value);
    formData.append('description', this.productForm.get('description')?.value);
    formData.append('price', this.productForm.get('price')?.value);
    formData.append('productTypeId', this.productForm.get('productTypeId')?.value);
    formData.append('productBrandId', this.productForm.get('productBrandId')?.value);

    this.selectedFiles.forEach(file => {
      formData.append('pictures', file);
    });

    this.shopService.editProduct(this.productId, formData).subscribe({
      next: () => {
        this.toastr.success('Produsul a fost actualizat!');
        this.selectedFiles = [];
        this.imagePreviews = [];
        this.loadProduct();
      },
      error: error => {
        console.log(error);
        this.toastr.error('Eroare la actualizare');
      }
    });
  }
}