import { Component, OnInit } from '@angular/core';
import { ShopService } from 'src/app/shop/shop.service';
import { ToastrService } from 'ngx-toastr'; // <-- Import nou

@Component({
  selector: 'app-my-products',
  templateUrl: './my-products.component.html',
  styleUrls: ['./my-products.component.scss']
})
export class MyProductsComponent implements OnInit {
  products: any[] = [];

  // Am adăugat toastr aici
  constructor(private shopService: ShopService, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.loadMyProducts();
  }

  loadMyProducts() {
    this.shopService.getMyProducts().subscribe({
      next: response => this.products = response,
      error: error => console.log(error)
    });
  }

  // --- LOGICA NOUĂ PENTRU ȘTERGERE ---
  deleteProduct(id: number) {
    // Afișăm un mesaj de confirmare browserului ca să nu șteargă din greșeală
    if (confirm('Are you sure you want to permanently delete this product?')) {
      this.shopService.deleteProduct(id).subscribe({
        next: () => {
          // Scoatem instantaneu produsul din listă (ca să nu mai dăm refresh la pagină)
          this.products = this.products.filter(p => p.id !== id);
          this.toastr.success('Product deleted successfully!');
        },
        error: error => {
          console.log(error);
          this.toastr.error('An error occurred while deleting.');
        }
      });
    }
  }
}
