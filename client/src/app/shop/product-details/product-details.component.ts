import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs';
import { BreadcrumbService } from 'xng-breadcrumb';
import {
  NgxGalleryAnimation,
  NgxGalleryImage,
  NgxGalleryImageSize,
  NgxGalleryOptions
} from '@kolkov/ngx-gallery';

import { Product } from 'src/app/shared/models/product';
import { ProductReview } from 'src/app/shared/models/review';
import { ShopService } from '../shop.service';
import { BasketService } from 'src/app/basket/basket.service';
import { AccountService } from 'src/app/account/account.service';
import { ReviewService } from 'src/app/core/services/review.service';

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

  suggestedProducts: Product[] = [];

  galleryOptions!: NgxGalleryOptions[];
  galleryImages: NgxGalleryImage[] = [];

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
    this.galleryOptions = [
  {
    width: '100%',
    height: '500px',
    imagePercent: 80,
    thumbnailsColumns: 4,
    imageAnimation: NgxGalleryAnimation.Fade,

    // MODIFICAT: poza principală este cropuită frumos în container
    imageSize: NgxGalleryImageSize.Cover,

    // thumbnails tot cropuite
    thumbnailSize: NgxGalleryImageSize.Cover,

    // MODIFICAT: la click se deschide poza mare/integrală
    preview: true,
    previewZoom: true,
    previewRotate: true
  }
];
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      if (user) this.currentUserEmail = user.email;
    });

    this.activatedRoute.paramMap.subscribe(params => {
      const id = params.get('id');

      if (id) {
        this.loadProduct(+id);
      }
    });
  }

  loadProduct(id: number) {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });

    this.product = undefined;
    this.galleryImages = [];
    this.productReviews = [];
    this.suggestedProducts = [];
    this.avgRating = 0;
    this.quantity = 1;
    this.quantityInBasket = 0;

    this.shopService.getProduct(id).subscribe({
      next: product => {
        this.product = product;
        this.bcService.set('@productDetails', product.name);

        // MODIFICAT: acum folosește pozele multiple dacă există
        this.galleryImages = this.getImages();

        this.basketService.basketSource$.pipe(take(1)).subscribe({
          next: basket => {
            const item = basket?.items.find(x => x.id === id);

            if (item) {
              this.quantity = item.quantity;
              this.quantityInBasket = item.quantity;
            }
          }
        });

        this.reviewService.getProductReviews(id).subscribe({
          next: (reviews: ProductReview[]) => {
            this.productReviews = reviews;

            this.avgRating = reviews.length > 0
              ? Math.round(
                  reviews.reduce((sum: number, r: ProductReview) => sum + r.rating, 0) /
                  reviews.length *
                  10
                ) / 10
              : 0;
          },
          error: err => console.log(err)
        });

        const text = product.name + ' ' + (product.description || '');
        const keywords = this.shopService.extractKeywords(text);

        if (keywords) {
          this.shopService.getSuggestions(keywords, id, 4).subscribe({
            next: suggestions => this.suggestedProducts = suggestions,
            error: err => console.log(err)
          });
        }
      },
      error: error => console.log(error)
    });
  }

  // MODIFICAT: nu mai duplică aceeași poză de 3 ori
  getImages(): NgxGalleryImage[] {
    const uniqueUrls = new Set<string>();

    if (this.product?.photos && this.product.photos.length > 0) {
      this.product.photos
        .slice()
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .forEach(photo => {
          if (photo.url) uniqueUrls.add(photo.url);
        });
    }

    // fallback pentru produsele vechi, care au doar pictureUrl
    if (uniqueUrls.size === 0 && this.product?.pictureUrl) {
      uniqueUrls.add(this.product.pictureUrl);
    }

    return Array.from(uniqueUrls).map(url => ({
      small: url,
      medium: url,
      big: url
    }));
  }

  // MODIFICAT: transformă stringul "Oily,Dry" în listă curată
  formatList(value?: string): string[] {
    if (!value) return [];

    return value
      .split(',')
      .map(x => x.trim())
      .filter(x => x.length > 0);
  }

  contactSeller() {
    if (this.product?.producerEmail) {
      this.router.navigate(['/chat'], {
        queryParams: { user: this.product.producerEmail }
      });
    }
  }

  incrementQuantity() {
    this.quantity++;
  }

  decrementQuantity() {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }

  updateBasket() {
    if (!this.product) return;

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

  get buttonText() {
    return this.quantityInBasket === 0 ? 'Add to basket' : 'Update basket';
  }
}
