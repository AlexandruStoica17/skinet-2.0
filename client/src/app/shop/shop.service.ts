import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Pagination } from '../shared/models/pagination';
import { Product } from '../shared/models/product';
import { Brand } from '../shared/models/brand';
import { Type } from '../shared/models/type';
import { ShopParams } from '../shared/models/shopParams';
import { map, of, switchMap, tap } from 'rxjs';

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

  // RESTAURAT: cache-ul pentru pagina Shop, similar cu proiectul SkiNet inițial
  productCache = new Map<string, Pagination<Product[]>>();

  // NOU: cache separat pentru produsele de pe Home
  homeProductsCache = new Map<string, Product[]>();

  constructor(private http: HttpClient) {}

  getProducts(useCache = true) {
    // RESTAURAT: dacă apelăm getProducts(false), curățăm cache-ul
    if (!useCache) {
      this.productCache = new Map<string, Pagination<Product[]>>();
    }

    const shopParams = this.getShopParams();
    const cacheKey = this.buildShopCacheKey(shopParams);

    // RESTAURAT: dacă avem deja rezultatul pentru acești parametri,
    // îl returnăm din cache, fără request nou către backend.
    if (useCache && this.productCache.has(cacheKey)) {
      const cachedResponse = this.productCache.get(cacheKey);

      if (cachedResponse) {
        this.pagination = cachedResponse;
        return of(cachedResponse);
      }
    }

    let params = new HttpParams();

    if (shopParams.brandId !== 0) {
      params = params.append('brandId', shopParams.brandId.toString());
    }

    if (shopParams.typeId !== 0) {
      params = params.append('typeId', shopParams.typeId.toString());
    }

    if (shopParams.search) {
      params = params.append('search', shopParams.search);
    }

    // Price range
    if (shopParams.minPrice > 0) {
      params = params.append('minPrice', shopParams.minPrice.toString());
    }

    if (shopParams.maxPrice > 0) {
      params = params.append('maxPrice', shopParams.maxPrice.toString());
    }

    // MULTISELECT: trimitem valorile ca string separat prin virgulă
    if (shopParams.skinTypes && shopParams.skinTypes.length > 0) {
      params = params.append('skinTypes', shopParams.skinTypes.join(','));
    }

    if (shopParams.usages && shopParams.usages.length > 0) {
      params = params.append('usages', shopParams.usages.join(','));
    }

    if (shopParams.benefits && shopParams.benefits.length > 0) {
      params = params.append('benefits', shopParams.benefits.join(','));
    }

    if (shopParams.formulas && shopParams.formulas.length > 0) {
      params = params.append('formulas', shopParams.formulas.join(','));
    }

    if (shopParams.minRating > 0) {
      params = params.append('minRating', shopParams.minRating.toString());
    }

    params = params.append('sort', shopParams.sort);
    params = params.append('pageIndex', shopParams.pageNumber.toString());
    params = params.append('pageSize', shopParams.pageSize.toString());

    return this.http.get<Pagination<Product[]>>(this.baseUrl + 'products', { params }).pipe(
      map(response => {
        // RESTAURAT: salvăm rezultatul în cache folosind cheia parametrilor actuali
        this.productCache.set(cacheKey, response);
        this.pagination = response;

        return response;
      })
    );
  }

  setShopParams(params: ShopParams) {
    this.shopParams = params;
  }

  getShopParams() {
    return this.shopParams;
  }

  getProduct(id: number) {
    // RESTAURAT/ADAPTAT: caută produsul în cache-ul paginilor deja încărcate
    const product = [...this.productCache.values()]
      .reduce((acc: Product | null, paginatedResult: Pagination<Product[]>) => {
        const found = paginatedResult.data.find((x: Product) => x.id === id);
        return found ?? acc;
      }, null as Product | null);

    if (product) {
      return of(product);
    }

    return this.http.get<Product>(this.baseUrl + 'products/' + id);
  }

  getBrands() {
    if (this.brands.length > 0) {
      return of(this.brands);
    }

    return this.http.get<Brand[]>(this.baseUrl + 'products/brands').pipe(
      map(brands => this.brands = brands)
    );
  }

  getTypes() {
    if (this.types.length > 0) {
      return of(this.types);
    }

    return this.http.get<Type[]>(this.baseUrl + 'products/types').pipe(
      map(types => this.types = types)
    );
  }

  addProduct(formData: FormData) {
    return this.http.post(this.baseUrl + 'products/add-product', formData).pipe(
      tap(() => this.clearProductCaches())
    );
  }

  getMyProducts() {
    return this.http.get<any[]>(this.baseUrl + 'products/my-products');
  }

  deleteProduct(id: number) {
    return this.http.delete(this.baseUrl + 'products/delete-product/' + id).pipe(
      tap(() => this.clearProductCaches())
    );
  }

  editProduct(id: number, formData: FormData) {
    return this.http.put(this.baseUrl + 'products/edit-product/' + id, formData).pipe(
      tap(() => this.clearProductCaches())
    );
  }

  getRecentProducts(count: number = 12) {
    return this.http.get<Product[]>(this.baseUrl + 'products/recent?count=' + count);
  }

  getSuggestions(keywords: string, excludeId: number = 0, count: number = 4) {
    const params = new HttpParams()
      .set('keywords', keywords)
      .set('excludeId', excludeId.toString())
      .set('count', count.toString());

    return this.http.get<Product[]>(this.baseUrl + 'products/suggestions', { params });
  }

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
    return this.http.put(this.baseUrl + `products/set-main-photo/${productId}/${photoId}`, {}).pipe(
      tap(() => this.clearProductCaches())
    );
  }

  deleteProductPhoto(productId: number, photoId: number) {
    return this.http.delete(this.baseUrl + `products/delete-photo/${productId}/${photoId}`).pipe(
      tap(() => this.clearProductCaches())
    );
  }

  moveProductPhoto(productId: number, photoId: number, direction: 'up' | 'down') {
    return this.http.put(
      this.baseUrl + `products/move-photo/${productId}/${photoId}?direction=${direction}`,
      {}
    ).pipe(
      tap(() => this.clearProductCaches())
    );
  }

  getHomeProducts(typeName: string, count: number = 3) {
    const cacheKey = `${typeName.toLowerCase()}-${count}`;

    if (this.homeProductsCache.has(cacheKey)) {
      return of(this.homeProductsCache.get(cacheKey)!);
    }

    return this.getTypes().pipe(
      map(types => {
        const type = types.find(t => t.name.toLowerCase() === typeName.toLowerCase());
        return type?.id ?? 0;
      }),

      switchMap(typeId => {
        let params = new HttpParams()
          .set('pageIndex', '1')
          .set('pageSize', count.toString())
          .set('sort', 'name');

        if (typeId !== 0) {
          params = params.set('typeId', typeId.toString());
        }

        return this.http.get<Pagination<Product[]>>(this.baseUrl + 'products', { params });
      }),

      map(response => response.data),

      tap(products => {
        this.homeProductsCache.set(cacheKey, products);
      })
    );
  }

  // NOU: cheie unică pentru cache, ținând cont de toate filtrele din shopParams
  private buildShopCacheKey(params: ShopParams): string {
    return [
      params.brandId,
      params.typeId,
      params.sort,
      params.pageNumber,
      params.pageSize,
      params.search,
      params.minPrice,
      params.maxPrice,
      (params.skinTypes ?? []).join('|'),
      (params.usages ?? []).join('|'),
      (params.benefits ?? []).join('|'),
      (params.formulas ?? []).join('|'),
      params.minRating
    ].join('-');
  }

  // NOU: când se modifică produse/poze, golim cache-ul ca să nu rămână date vechi
  private clearProductCaches() {
    this.productCache.clear();
    this.homeProductsCache.clear();
  }
}