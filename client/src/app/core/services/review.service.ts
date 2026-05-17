import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { ProducerReviewsResponse, ProductReview } from 'src/app/shared/models/review';


@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // Aduce review-urile pentru un produs
  getProductReviews(productId: number) {
    return this.http.get<ProductReview[]>(this.baseUrl + 'reviews/product/' + productId);
  }

  // Aduce review-urile pentru un vanzator
  getProducerReviews(email: string) {
    return this.http.get<ProducerReviewsResponse>(
      this.baseUrl + 'reviews/producer?email=' + encodeURIComponent(email)
    );
  }

  // Trimite review pentru un produs
  submitProductReview(data: { productId: number; orderId: number; rating: number; comment: string }) {
    return this.http.post(this.baseUrl + 'reviews/product', data);
  }
}