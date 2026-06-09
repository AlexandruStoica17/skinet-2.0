import { Component, Input, OnInit } from '@angular/core';
import { Product } from 'src/app/shared/models/product';
import { BasketService } from 'src/app/basket/basket.service';
import { LikesService } from 'src/app/core/services/likes.service';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from 'src/app/account/account.service';
import { take } from 'rxjs';

@Component({
  selector: 'app-product-item',
  templateUrl: './product-item.component.html',
  styleUrls: ['./product-item.component.scss']
})
export class ProductItemComponent implements OnInit {
  @Input() product?: Product;
  isFavorite = false;

  constructor(
    private basketService: BasketService,
    private likesService: LikesService,
    private accountService: AccountService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
  }

  addItemToBasket() {
    this.product && this.basketService.addItemToBasket(this.product);
  }

  addLike() {
    if (!this.product) {
      return;
    }

    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (!user) {
          this.toastr.error('You need to be logged in to add favorites!');
          return;
        }

        this.likesService.addLike(this.product!.id, user.email).subscribe({
          next: (response: any) => {
            this.isFavorite = true;

            if (response?.alreadyExists) {
              this.toastr.info(this.product?.name + ' is already in your favorites.');
              return;
            }

            this.toastr.success('Added ' + this.product?.name + ' to favorites!');
          },
          error: error => {
            const message = error?.error?.message || error?.message || '';

            if (message.includes('Product is already in favorites')) {
              this.isFavorite = true;
              this.toastr.info(this.product?.name + ' is already in your favorites.');
            } else {
              this.toastr.error('An error occurred while adding.');
            }

            console.log(error);
          }
        });
      }
    });
  }
}
