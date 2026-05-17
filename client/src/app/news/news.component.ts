import { Component, OnInit } from '@angular/core';
import { Product } from '../shared/models/product';
import { ShopService } from '../shop/shop.service';

@Component({
  selector: 'app-news',
  templateUrl: './news.component.html',
  styleUrls: ['./news.component.scss']
})
export class NewsComponent implements OnInit {
  // Recently added products loaded from API
  recentProducts: Product[] = [];
  loading = false;

  constructor(private shopService: ShopService) { }

  ngOnInit(): void {
    this.loadRecentProducts();
  }

  loadRecentProducts() {
    this.loading = true;
    this.shopService.getRecentProducts(12).subscribe({
      next: products => {
        this.recentProducts = products;
        this.loading = false;
      },
      error: err => {
        console.log(err);
        this.loading = false;
      }
    });
  }
}