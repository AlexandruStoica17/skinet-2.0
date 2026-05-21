import { Component, OnInit } from '@angular/core';
import { Product } from '../shared/models/product';
import { ShopService } from '../shop/shop.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  cosmetics: Product[] = [];
  ingredients: Product[] = [];

  loadingCosmetics = false;
  loadingIngredients = false;

  constructor(private shopService: ShopService) { }

  ngOnInit(): void {
    this.getCosmetics();
    this.getIngredients();
  }

  getCosmetics() {
    this.loadingCosmetics = true;

    this.shopService.getHomeProducts('Cosmetics', 3).subscribe({
      next: products => {
        this.cosmetics = products;
        this.loadingCosmetics = false;
      },
      error: error => {
        console.log(error);
        this.loadingCosmetics = false;
      }
    });
  }

  getIngredients() {
    this.loadingIngredients = true;

    this.shopService.getHomeProducts('Ingredients', 3).subscribe({
      next: products => {
        this.ingredients = products;
        this.loadingIngredients = false;
      },
      error: error => {
        console.log(error);
        this.loadingIngredients = false;
      }
    });
  }
}