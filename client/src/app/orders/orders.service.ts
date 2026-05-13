import { HttpClient } from '@angular/common/http'; 
import { Injectable } from '@angular/core'; 
import { environment } from 'src/environments/environment'; 
import { Order } from '../shared/models/order'; 
 
@Injectable({ 
  providedIn: 'root' 
}) 
export class OrdersService { 
  baseUrl = environment.apiUrl; 
 
  constructor(private http: HttpClient) { } 
 
  getOrdersForUser() { 
    return this.http.get<Order[]>(this.baseUrl + 'orders'); 
  } 
 
  getOrderDetailed(id: number) { 
    return this.http.get<Order>(this.baseUrl + 'orders/' + id); 
  } 

  getOrdersForProducer() {
    return this.http.get<any[]>(this.baseUrl + 'orders/producer-orders');
  }

  // NOU: cumparatorul confirma primirea comenzii
  markOrderAsDelivered(orderId: number) {
    return this.http.put<Order>(this.baseUrl + 'orders/mark-delivered/' + orderId, {});
  }
}