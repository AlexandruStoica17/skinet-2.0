import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AccountService } from '../account/account.service';
import { BasketService } from '../basket/basket.service';
import { SellerProfile } from '../shared/models/sellerProfile';
import { Product } from '../shared/models/product';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-seller-profile',
  templateUrl: './seller-profile.component.html',
  styleUrls: ['./seller-profile.component.scss']
})
export class SellerProfileComponent implements OnInit {
  seller?: SellerProfile;
  sellerEmail = '';
  pageNumber = 1;
  pageSize = 8;
  loading = false;
  mapUrl?: SafeResourceUrl;

  constructor(
    private route: ActivatedRoute,
    private accountService: AccountService,
    private basketService: BasketService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const email = params.get('email');
      if (!email) return;

      this.sellerEmail = decodeURIComponent(email);
      this.pageNumber = 1;
      this.loadSeller();
    });
  }

  loadSeller() {
    this.loading = true;

    this.accountService.getSellerProfile(this.sellerEmail, this.pageNumber, this.pageSize).subscribe({
      next: seller => {
        this.seller = seller;
        this.mapUrl = seller.mapUrl
          ? this.sanitizer.bypassSecurityTrustResourceUrl(seller.mapUrl)
          : undefined;
        this.loading = false;
      },
      error: error => {
        console.log(error);
        this.loading = false;
      }
    });
  }

  onPageChanged(page: number) {
    if (this.pageNumber !== page) {
      this.pageNumber = page;
      this.loadSeller();
      window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
    }
  }

  addItemToBasket(product: Product) {
    this.basketService.addItemToBasket(product);
  }

  getDocumentUrl(documentUrl?: string) {
    if (!documentUrl) return '';
    return environment.apiUrl.replace('/api/', '/') + documentUrl;
  }
}
