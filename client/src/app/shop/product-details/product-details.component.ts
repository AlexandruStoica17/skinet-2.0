import { Component, OnInit } from '@angular/core';
import { Product } from 'src/app/shared/models/product';
import { ShopService } from '../shop.service';
import { ActivatedRoute } from '@angular/router';
import { BreadcrumbService } from 'xng-breadcrumb';
import { BasketService } from 'src/app/basket/basket.service';
import { take } from 'rxjs';
// Importurile adăugate pentru galerie
import { NgxGalleryAnimation, NgxGalleryImage, NgxGalleryOptions, NgxGalleryImageSize } from '@kolkov/ngx-gallery';

@Component({
  selector: 'app-product-details',
  templateUrl: './product-details.component.html',
  styleUrls: ['./product-details.component.scss']
})
export class ProductDetailsComponent implements OnInit {
  product?: Product; 
  quantity = 1;
  quantityInBasket = 0;
  
  // Variabile adăugate pentru galerie
  galleryOptions!: NgxGalleryOptions[];
  galleryImages!: NgxGalleryImage[];

  constructor(
    private shopService: ShopService, 
    private activatedRoute: ActivatedRoute,
    private bcService: BreadcrumbService, 
    private basketService: BasketService
  ){
    this.bcService.set('@productDetails', ' ')
  }

  ngOnInit(): void {
    this.loadProduct();
    
    // Inițializăm opțiunile vizuale ale galeriei
   this.galleryOptions = [
      {
        width: '100%',
        height: '500px',
        imagePercent: 80, // Poza mare ia 80% din înălțime
        thumbnailsColumns: 4,
        imageAnimation: NgxGalleryAnimation.Fade, // Fade arată mai fin decât Slide
        imageSize: NgxGalleryImageSize.Contain, // REZOLVĂ POZA TĂIATĂ
        thumbnailSize: NgxGalleryImageSize.Contain,
        preview: false
      }
    ];
  }

  loadProduct(){
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (id) this.shopService.getProduct(+id).subscribe({
      next: product => {
        this.product = product;
        this.bcService.set('@productDetails', product.name);
        
        // Generăm pozele pentru carusel Imediat ce avem detaliile produsului
        this.galleryImages = this.getImages();

        this.basketService.basketSource$.pipe(take(1)).subscribe({
          next: basket => {
            const item = basket?.items.find(x => x.id === +id);
            if(item){
              this.quantity = item.quantity;
              this.quantityInBasket = item.quantity;
            }
          }
        })
      },
      error: error => console.log(error)
    })
  }

  // Funcție nouă: Creează un array de poze pentru galerie
getImages(): NgxGalleryImage[] {
    const imageUrls: NgxGalleryImage[] = [];
    if (this.product) {
      // Adăugăm poza de 3 ori ca să vedem galeria frumos
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

  incrementQuantity(){
    this.quantity++;
  }

  decrementQuantity(){
    this.quantity--;
  }

  updateBasket(){
    if(this.product){
      if(this.quantity > this.quantityInBasket){
        const itemsToAdd = this.quantity - this.quantityInBasket;
        this.quantityInBasket += itemsToAdd;
        this.basketService.addItemToBasket(this.product, itemsToAdd);
      }
      else{
        const itemsToRemove = this.quantityInBasket - this.quantity;
        this.quantityInBasket -= itemsToRemove;
        this.basketService.removeItemFromBasket(this.product.id, itemsToRemove);
      }
    }
  }

  get buttonText(){
    return this.quantityInBasket === 0 ? 'Add to basket' : 'Update basket';
  }
}