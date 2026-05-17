import { Product } from 'src/app/shared/models/product';
import { ProductReview } from 'src/app/shared/models/review';
import { ShopService } from '../shop.service';
import { ActivatedRoute, Router } from '@angular/router';
import { BreadcrumbService } from 'xng-breadcrumb';
import { BasketService } from 'src/app/basket/basket.service';
import { AccountService } from 'src/app/account/account.service';
import { ReviewService } from 'src/app/core/services/review.service';
import { take } from 'rxjs';
import { NgxGalleryAnimation, NgxGalleryImage, NgxGalleryOptions, NgxGalleryImageSize } from '@kolkov/ngx-gallery';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-product-details',
  templateUrl: './product-details.component.html',
  styleUrls: ['./product-details.component.scss']
})
export class ProductDetailsComponent implements OnInit {
  product?: Product;
  quantity = 1;
  quantityInBasket = 0;
  currentUserEmail = '';

  productReviews: ProductReview[] = [];
  avgRating = 0;

  // NEW: related product suggestions based on name/description keywords
  suggestedProducts: Product[] = [];

  galleryOptions!: NgxGalleryOptions[];
  galleryImages!: NgxGalleryImage[];

  constructor(
    private shopService: ShopService,
    private activatedRoute: ActivatedRoute,
    private bcService: BreadcrumbService,
    private basketService: BasketService,
    private accountService: AccountService,
    private router: Router,
    private reviewService: ReviewService
  ) {
    this.bcService.set('@productDetails', ' ');
  }

  ngOnInit(): void {
    this.loadProduct();

    this.galleryOptions = [
      {
        width: '100%',
        height: '500px',
        imagePercent: 80,
        thumbnailsColumns: 4,
        imageAnimation: NgxGalleryAnimation.Fade,
        imageSize: NgxGalleryImageSize.Contain,
        thumbnailSize: NgxGalleryImageSize.Contain,
        preview: false
      }
    ];

    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      if (user) this.currentUserEmail = user.email;
    });
  }

  loadProduct() {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (id) this.shopService.getProduct(+id).subscribe({
      next: product => {
        this.product = product;
        this.bcService.set('@productDetails', product.name);
        this.galleryImages = this.getImages();

        this.basketService.basketSource$.pipe(take(1)).subscribe({
          next: basket => {
            const item = basket?.items.find(x => x.id === +id);
            if (item) {
              this.quantity = item.quantity;
              this.quantityInBasket = item.quantity;
            }
          }
        });

        // Load reviews for this product
        this.reviewService.getProductReviews(+id).subscribe({
          next: (reviews: ProductReview[]) => {
            this.productReviews = reviews;
            this.avgRating = reviews.length > 0
              ? Math.round(
                  reviews.reduce((sum: number, r: ProductReview) => sum + r.rating, 0)
                  / reviews.length * 10
                ) / 10
              : 0;
          },
          error: err => console.log(err)
        });

        // NEW: Load related product suggestions based on keywords from name + description
        const text = product.name + ' ' + (product.description || '');
        const keywords = this.shopService.extractKeywords(text);
        if (keywords) {
          this.shopService.getSuggestions(keywords, +id, 4).subscribe({
            next: suggestions => this.suggestedProducts = suggestions,
            error: err => console.log(err)
          });
        }
      },
      error: error => console.log(error)
    });
  }

  getImages(): NgxGalleryImage[] {
    const imageUrls: NgxGalleryImage[] = [];
    if (this.product) {
      for (let i = 0; i < 3; i++) {
        imageUrls.push({
          small: this.product.pictureUrl,
          medium: this.product.pictureUrl,
          big: this.product.pictureUrl
        });
      }
    }
    return imageUrls;
  }

  contactSeller() {
    if (this.product?.producerEmail) {
      this.router.navigate(['/chat'], {
        queryParams: { user: this.product.producerEmail }
      });
    }
  }

  incrementQuantity() { this.quantity++; }
  decrementQuantity() { this.quantity--; }

  updateBasket() {
    if (this.product) {
      if (this.quantity > this.quantityInBasket) {
        const itemsToAdd = this.quantity - this.quantityInBasket;
        this.quantityInBasket += itemsToAdd;
        this.basketService.addItemToBasket(this.product, itemsToAdd);
      } else {
        const itemsToRemove = this.quantityInBasket - this.quantity;
        this.quantityInBasket -= itemsToRemove;
        this.basketService.removeItemFromBasket(this.product.id, itemsToRemove);
      }
    }
  }

  get buttonText() {
    return this.quantityInBasket === 0 ? 'Add to basket' : 'Update basket';
  }
}