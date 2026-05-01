import { Component, OnInit } from '@angular/core';
import { Product } from '../shared/models/product';
import { LikesService } from '../core/services/likes.service';
import { AccountService } from '../account/account.service'; // Adăugat
import { take } from 'rxjs'; // Adăugat
import { ToastrService } from 'ngx-toastr'; // Adăugat

@Component({
  selector: 'app-favorites',
  templateUrl: './favorites.component.html',
  styleUrls: ['./favorites.component.scss']
})
export class FavoritesComponent implements OnInit {
  products: Product[] = [];

  constructor(
    private likesService: LikesService,
    private accountService: AccountService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.getFavorites();
  }

  getFavorites() {
    this.likesService.getLikes().subscribe({
      next: (response: any) => this.products = response,
      error: error => console.log(error)
    });
  }

  removeFavorite(productId: number) {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (user) {
          this.likesService.removeLike(productId, user.email).subscribe({
            next: () => {
              // Scoatem produsul din listă fără să dăm refresh la pagină
              this.products = this.products.filter(p => p.id !== productId);
              this.toastr.info('Produs eliminat de la favorite');
            },
            error: error => console.log(error)
          });
        }
      }
    });
  }
}