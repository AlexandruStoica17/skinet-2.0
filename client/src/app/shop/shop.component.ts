import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Product } from '../shared/models/product';
import { ShopService } from './shop.service';
import { Brand } from '../shared/models/brand';
import { Type } from '../shared/models/type';
import { ShopParams } from '../shared/models/shopParams';

type MultiFilterKey = 'skinTypes' | 'usages' | 'benefits' | 'formulas';
type FilterSectionKey = MultiFilterKey | 'ratings';

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

  usageOptions = [
    'Face',
    'Eyes',
    'Lips',
    'Hands',
    'Body',
    'Hair',
    'Neck & Décolletage',
  ];

  benefitOptions = [
    'Hydration',
    'Anti-aging',
    'SPF Protection',
    'Brightening',
    'Pore Cleansing',
    'Firming',
    'Soothing',
    'Exfoliating',
    'Nourishing',
    'Mattifying',
  ];

  formulaOptions = [
    'Cream',
    'Serum',
    'Lotion',
    'Emulsion',
    'Oil',
    'Gel',
    'Foam',
    'Toner',
    'Mask',
    'Scrub',
    'Liquid',
    'Balm',
    'Powder',
  ];

  // MODIFICAT: am adăugat 0, 1 și 2 stele.
  // Observație: 0 înseamnă "fără filtru minim", ca să nu rupem backend-ul existent.
  ratingOptions = [
    { label: '☆☆☆☆☆ 0 stars', value: 0 },
    { label: '★☆☆☆☆ 1+ star', value: 1 },
    { label: '★★☆☆☆ 2+ stars', value: 2 },
    { label: '★★★☆☆ 3+ stars', value: 3 },
    { label: '★★★★☆ 4+ stars', value: 4 },
    { label: '★★★★★ 5 stars', value: 5 },
  ];

  // MODIFICAT: filtrele lungi sunt închise inițial
  filterSectionsOpen: Record<FilterSectionKey, boolean> = {
    skinTypes: false,
    usages: false,
    benefits: false,
    formulas: false,
    ratings: false,
  };

  constructor(
    private shopService: ShopService,
    private route: ActivatedRoute
  ) {
    this.shopParams = shopService.getShopParams();
  }

  ngOnInit(): void {
    this.getBrands();

    this.route.queryParams.subscribe(params => {
      this.shopService.getTypes().subscribe(types => {
        this.types = [{ id: 0, name: 'All' }, ...types];

        const shopParams = this.shopService.getShopParams();
        const typeParam = params['type'];

        if (typeParam) {
          const found = types.find(
            (t: any) => t.name.toLowerCase() === typeParam.toLowerCase()
          );

          shopParams.typeId = found ? found.id : 0;
        } else {
          shopParams.typeId = 0;
        }

        shopParams.pageNumber = 1;
        this.shopService.setShopParams(shopParams);
        this.shopParams = shopParams;
        this.getProducts();
      });
    });
  }

  getProducts() {
    this.shopService.getProducts().subscribe({
      next: response => {
        this.products = response.data;
        this.totalCount = response.count;
      },
      error: err => console.log(err),
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

  onMinPriceChange(event: Event) {
    const val = +(event.target as HTMLInputElement).value;

    if (val <= this.priceMax) {
      this.priceMin = val;
      this.applyPriceFilter();
    }
  }

  onMaxPriceChange(event: Event) {
    const val = +(event.target as HTMLInputElement).value;

    if (val >= this.priceMin) {
      this.priceMax = val;
      this.applyPriceFilter();
    }
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

  // MODIFICAT: deschide/închide secțiunile lungi de filtre
  toggleFilterSection(section: FilterSectionKey) {
    this.filterSectionsOpen[section] = !this.filterSectionsOpen[section];
  }

  toggleFilter(paramKey: MultiFilterKey, value: string) {
    const params = this.shopService.getShopParams();
    const arr = params[paramKey] as string[];

    const idx = arr.indexOf(value);

    if (idx > -1) {
      arr.splice(idx, 1);
    } else {
      arr.push(value);
    }

    params.pageNumber = 1;

    this.shopService.setShopParams(params);
    this.shopParams = params;
    this.getProducts();
  }

  isSelected(paramKey: MultiFilterKey, value: string): boolean {
    return (this.shopParams[paramKey] as string[]).includes(value);
  }

  getSelectedCount(paramKey: MultiFilterKey): number {
    return (this.shopParams[paramKey] as string[]).length;
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
