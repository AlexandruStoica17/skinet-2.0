import { Component, Input, OnInit } from '@angular/core';
import { Product } from 'src/app/shared/models/product';
import { BasketService } from 'src/app/basket/basket.service';
import { LikesService } from 'src/app/core/services/likes.service';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from 'src/app/account/account.service'; // <--- IMPORT NOU
import { take } from 'rxjs'; // <--- IMPORT NOU

@Component({
  selector: 'app-product-item',
  templateUrl: './product-item.component.html',
  styleUrls: ['./product-item.component.scss']
})
export class ProductItemComponent implements OnInit {
  @Input() product?: Product;

  constructor(
    private basketService: BasketService,
    private likesService: LikesService,
    private accountService: AccountService, // <--- INJECTĂM AICI
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
  }

  addItemToBasket() {
    this.product && this.basketService.addItemToBasket(this.product);
  }

  addLike() {
    if (this.product) {
      // Extragem utilizatorul curent
      this.accountService.currentUser$.pipe(take(1)).subscribe({
        next: user => {
          if (user) {
            // Trimitem ID-ul produsului și Email-ul către C#
            this.likesService.addLike(this.product!.id, user.email).subscribe({
              next: () => this.toastr.success('Ai adăugat ' + this.product?.name + ' la favorite!'),
              error: (error) => {
                // Aici va pica dacă e deja adăugat (eroarea 400 setată de tine în C#)
                if (error.error?.message === "Produsul este deja la favorite") {
                  this.toastr.warning('Acest produs este deja în lista ta!');
                } else {
                  this.toastr.error('A apărut o eroare la adăugare.');
                }
                console.log(error);
              }
            });
          } else {
            // Dacă apasă pe inimă fără să fie logat
            this.toastr.error('Trebuie să fii logat pentru a adăuga la favorite!');
          }
        }
      });
    }
  }
}