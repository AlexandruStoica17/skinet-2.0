import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Product } from '../shared/models/product';
import { ShopService } from './shop.service';
import { Brand } from '../shared/models/brand';
import { Type } from '../shared/models/type';
import { ShopParams } from '../shared/models/shopParams';

@Component({
  selector: 'app-shop',
  templateUrl: './shop.component.html',
  styleUrls: ['./shop.component.scss'],
})
export class ShopComponent implements OnInit {
  @ViewChild('search') searchTerm?: ElementRef;
  products: Product[] = [];
  brands: Brand[] = [];
  types: Type[] = [];
  shopParams: ShopParams;

  sortOptions = [
    { name: 'Alphabetical', value: 'name' },
    { name: 'Price: Low to high', value: 'priceAsc' },
    { name: 'Price: High to low', value: 'priceDesc' },
  ];
  totalCount = 0;

  // Price range
  priceMin = 0;
  priceMax = 1000;
  readonly PRICE_ABSOLUTE_MAX = 1000;

  // Filter options
  skinTypeOptions = ['All Skin Types', 'Oily', 'Dry', 'Combination', 'Sensitive', 'Normal'];
  usageOptions    = ['Face', 'Eyes', 'Lips', 'Hands', 'Body', 'Hair', 'Neck & Décolletage'];
  benefitOptions  = ['Hydration', 'Anti-aging', 'SPF Protection', 'Brightening',
                     'Pore Cleansing', 'Firming', 'Soothing', 'Exfoliating',
                     'Nourishing', 'Mattifying'];
  formulaOptions  = ['Cream', 'Serum', 'Lotion', 'Emulsion', 'Oil', 'Gel',
                     'Foam', 'Toner', 'Mask', 'Scrub', 'Liquid', 'Balm', 'Powder'];

  ratingOptions = [
    { label: '⭐⭐⭐⭐⭐ 5 stars', value: 5 },
    { label: '⭐⭐⭐⭐ 4+ stars', value: 4 },
    { label: '⭐⭐⭐ 3+ stars', value: 3 },
  ];

  constructor(
    private shopService: ShopService,
    private route: ActivatedRoute
  ) {
    this.shopParams = shopService.getShopParams();
  }

  ngOnInit(): void {
    this.getBrands();
    this.route.queryParams.subscribe(params => {
      if (params['type']) {
        this.shopService.getTypes().subscribe(types => {
          this.types = [{ id: 0, name: 'All' }, ...types];
          const found = types.find((t: any) =>
            t.name.toLowerCase() === params['type'].toLowerCase());
          if (found) {
            this.shopParams.typeId = found.id;
            this.shopService.setShopParams(this.shopParams);
          }
          this.getProducts();
        });
      } else {
        this.getTypes();
        this.getProducts();
      }
    });
  }

  getProducts() {
    this.shopService.getProducts().subscribe({
      next: response => {
        this.products = response.data;
        this.totalCount = response.count;
      },
    });
  }

  getBrands() {
    this.shopService.getBrands().subscribe({
      next: response => (this.brands = [{ id: 0, name: 'All' }, ...response]),
      error: err => console.log(err),
    });
  }

  getTypes() {
    this.shopService.getTypes().subscribe({
      next: response => (this.types = [{ id: 0, name: 'All' }, ...response]),
      error: err => console.log(err),
    });
  }

  onBrandSelected(brandId: number) {
    const params = this.shopService.getShopParams();
    params.brandId = brandId;
    params.pageNumber = 1;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  onTypeSelected(typeId: number) {
    const params = this.shopService.getShopParams();
    params.typeId = typeId;
    params.pageNumber = 1;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  onSortSelected(event: Event) {
    const params = this.shopService.getShopParams();
    params.sort = (event.target as HTMLSelectElement).value;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  // Price range
  onMinPriceChange(event: Event) {
    const val = +(event.target as HTMLInputElement).value;
    if (val <= this.priceMax) { this.priceMin = val; this.applyPriceFilter(); }
  }

  onMaxPriceChange(event: Event) {
    const val = +(event.target as HTMLInputElement).value;
    if (val >= this.priceMin) { this.priceMax = val; this.applyPriceFilter(); }
  }

  applyPriceFilter() {
    const params = this.shopService.getShopParams();
    params.minPrice = this.priceMin;
    params.maxPrice = this.priceMax >= this.PRICE_ABSOLUTE_MAX ? 0 : this.priceMax;
    params.pageNumber = 1;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  // MULTISELECT toggle helper
  // Adds or removes value from the selected array, then reloads products
  toggleFilter(paramKey: 'skinTypes' | 'usages' | 'benefits' | 'formulas', value: string) {
    const params = this.shopService.getShopParams();
    const arr = params[paramKey] as string[];
    const idx = arr.indexOf(value);
    if (idx > -1) {
      arr.splice(idx, 1);   // deselect
    } else {
      arr.push(value);       // select
    }
    params.pageNumber = 1;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  // Check if a value is selected (used in HTML for [class.active])
  isSelected(paramKey: 'skinTypes' | 'usages' | 'benefits' | 'formulas', value: string): boolean {
    return (this.shopParams[paramKey] as string[]).includes(value);
  }

  onRatingSelected(minRating: number) {
    const params = this.shopService.getShopParams();
    params.minRating = params.minRating === minRating ? 0 : minRating;
    params.pageNumber = 1;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  onSearch() {
    const params = this.shopService.getShopParams();
    params.search = this.searchTerm?.nativeElement.value;
    params.pageNumber = 1;
    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  onReset() {
    if (this.searchTerm) this.searchTerm.nativeElement.value = '';
    this.priceMin = 0;
    this.priceMax = this.PRICE_ABSOLUTE_MAX;
    this.shopParams = new ShopParams();
    this.shopService.setShopParams(this.shopParams);
    this.getProducts();
  }

  onPageChanged(event: number) {
    const params = this.shopService.getShopParams();
    if (params.pageNumber !== event) {
      params.pageNumber = event;
      this.shopService.setShopParams(params);
      this.shopParams = params;
      this.getProducts();
    }
  }
}