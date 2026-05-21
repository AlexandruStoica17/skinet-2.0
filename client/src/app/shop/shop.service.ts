import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Pagination } from '../shared/models/pagination';
import { Product } from '../shared/models/product';
import { Brand } from '../shared/models/brand';
import { Type } from '../shared/models/type';
import { ShopParams } from '../shared/models/shopParams';
import { map, of } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ShopService {
  baseUrl = 'https://localhost:5001/api/';
  products: Product[] = [];
  brands: Brand[] = [];
  types: Type[] = [];
  pagination?: Pagination<Product[]>;
  shopParams = new ShopParams();
  productCache = new Map<string, Pagination<Product[]>>();

  constructor(private http: HttpClient) {}

  getProducts(useCache = false) {
    let params = new HttpParams();
    const shopParams = this.getShopParams();

    if (shopParams.brandId !== 0) params = params.append('brandId', shopParams.brandId.toString());
    if (shopParams.typeId !== 0)  params = params.append('typeId', shopParams.typeId.toString());
    if (shopParams.search)        params = params.append('search', shopParams.search);

    // Price range
    if (shopParams.minPrice > 0)  params = params.append('minPrice', shopParams.minPrice.toString());
    if (shopParams.maxPrice > 0)  params = params.append('maxPrice', shopParams.maxPrice.toString());

    // MULTISELECT: send as comma-separated strings
    if (shopParams.skinTypes && shopParams.skinTypes.length > 0)
      params = params.append('skinTypes', shopParams.skinTypes.join(','));
    if (shopParams.usages && shopParams.usages.length > 0)
      params = params.append('usages', shopParams.usages.join(','));
    if (shopParams.benefits && shopParams.benefits.length > 0)
      params = params.append('benefits', shopParams.benefits.join(','));
    if (shopParams.formulas && shopParams.formulas.length > 0)
      params = params.append('formulas', shopParams.formulas.join(','));

    if (shopParams.minRating > 0)
      params = params.append('minRating', shopParams.minRating.toString());

    params = params.append('sort', shopParams.sort);
    params = params.append('pageIndex', shopParams.pageNumber.toString());
    params = params.append('pageSize', shopParams.pageSize.toString());

    // FIX: Pagination<Product[]> — data is an array
    return this.http.get<Pagination<Product[]>>(this.baseUrl + 'products', { params });
  }

  setShopParams(params: ShopParams) {
    this.shopParams = params;
  }

  getShopParams() {
    return this.shopParams;
  }

  getProduct(id: number) {
    // FIX: explicitly type the accumulated value and handle Product[] correctly
    const product = [...this.productCache.values()]
      .reduce((acc: Product | null, paginatedResult: Pagination<Product[]>) => {
        const found = paginatedResult.data.find((x: Product) => x.id === id);
        return found ?? acc;
      }, null as Product | null);

    if (product) return of(product);

    return this.http.get<Product>(this.baseUrl + 'products/' + id);
  }

  getBrands() {
    if (this.brands.length > 0) return of(this.brands);
    return this.http.get<Brand[]>(this.baseUrl + 'products/brands').pipe(
      map(brands => this.brands = brands)
    );
  }

  getTypes() {
    if (this.types.length > 0) return of(this.types);
    return this.http.get<Type[]>(this.baseUrl + 'products/types').pipe(
      map(types => this.types = types)
    );
  }

  addProduct(formData: FormData) {
    return this.http.post(this.baseUrl + 'products/add-product', formData);
  }

  getMyProducts() {
    return this.http.get<any[]>(this.baseUrl + 'products/my-products');
  }

  deleteProduct(id: number) {
    return this.http.delete(this.baseUrl + 'products/delete-product/' + id);
  }

  editProduct(id: number, formData: FormData) {
    return this.http.put(this.baseUrl + 'products/edit-product/' + id, formData);
  }

  // What's New page
  getRecentProducts(count: number = 12) {
    return this.http.get<Product[]>(this.baseUrl + 'products/recent?count=' + count);
  }

  // Product suggestions based on keywords
  getSuggestions(keywords: string, excludeId: number = 0, count: number = 4) {
    const params = new HttpParams()
      .set('keywords', keywords)
      .set('excludeId', excludeId.toString())
      .set('count', count.toString());
    return this.http.get<Product[]>(this.baseUrl + 'products/suggestions', { params });
  }

  // Extract keywords from text for auto-suggestions
  extractKeywords(text: string): string {
    const stopWords = [
      'the', 'and', 'for', 'with', 'this', 'that', 'are', 'from', 'has', 'have',
      'will', 'can', 'your', 'our', 'its', 'been', 'also', 'more', 'very',
      'si', 'sau', 'cu', 'de', 'la', 'in', 'un', 'o', 'pe', 'pentru',
      'este', 'sunt', 'care', 'din', 'cel', 'mai', 'ale', 'al', 'lui', 'ei'
    ];

    return text
      .toLowerCase()
      .replace(/[^a-zăâîșț\s]/gi, ' ')
      .split(/\s+/)
      .filter(w => w.length > 3 && !stopWords.includes(w))
      .slice(0, 10)
      .join(',');
  }

  setMainPhoto(productId: number, photoId: number) {
  return this.http.put(this.baseUrl + `products/set-main-photo/${productId}/${photoId}`, {});
}

deleteProductPhoto(productId: number, photoId: number) {
  return this.http.delete(this.baseUrl + `products/delete-photo/${productId}/${photoId}`);
}

moveProductPhoto(productId: number, photoId: number, direction: 'up' | 'down') {
  return this.http.put(
    this.baseUrl + `products/move-photo/${productId}/${photoId}?direction=${direction}`,
    {}
  );
}
}