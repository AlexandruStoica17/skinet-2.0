import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map } from 'rxjs';
import { environment } from 'src/environments/environment';
import { DeliveryMethod } from '../shared/models/deliveryMethod';
import { Order, OrderToCreate } from '../shared/models/order';

@Injectable({
  providedIn: 'root',
})
export class CheckoutService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  createOrder(order: OrderToCreate) {
    return this.http.post<Order>(this.baseUrl + 'orders', order);
  }

  getDeliveryMethods() {
    return this.http
      .get<DeliveryMethod[]>(this.baseUrl + 'orders/deliveryMethods')
      .pipe(
        map((dm) => {
          return dm.sort((a, b) => b.price - a.price);
        }),
      );
  }

  getOrdersForProducer() {
    return this.http.get<Order[]>(this.baseUrl + 'orders/producer-orders');
  }

  // Adaugă asta lângă celelalte funcții
  markOrderAsShipped(orderId: number) {
    return this.http.put<Order>(
      this.baseUrl + 'orders/ship-order/' + orderId,
      {},
    );
  }
}
