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
  pageNumber = 1;
  pageSize = 8;
  totalCount = 0;

  constructor(private shopService: ShopService) { }

  ngOnInit(): void {
    this.loadRecentProducts();
  }

  loadRecentProducts() {
    this.loading = true;
    this.shopService.getRecentProducts(this.pageNumber, this.pageSize).subscribe({
      next: response => {
        this.recentProducts = response.data;
        this.pageNumber = response.pageIndex;
        this.pageSize = response.pageSize;
        this.totalCount = response.count;
        this.loading = false;
      },
      error: err => {
        console.log(err);
        this.loading = false;
      }
    });
  }

  onPageChanged(event: number) {
    if (this.pageNumber !== event) {
      this.pageNumber = event;
      this.loadRecentProducts();
      window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
    }
  }
}
