import { Component, OnInit } from '@angular/core';
import { Order } from 'src/app/shared/models/order';
import { CheckoutService } from 'src/app/checkout/checkout.service';

@Component({
  selector: 'app-producer-orders',
  templateUrl: './producer-orders.component.html'
})
export class ProducerOrdersComponent implements OnInit {
  orders: Order[] = [];
  selectedOrder: Order | null = null; // Aici stocăm comanda deschisă

  constructor(private checkoutService: CheckoutService) { }

  ngOnInit(): void {
    this.checkoutService.getOrdersForProducer().subscribe({
      next: orders => this.orders = orders,
      error: error => console.log(error)
    });
  }

  // Funcție pentru a deschide comanda
  viewOrder(order: Order) {
    this.selectedOrder = order;
  }

  // Funcție pentru a ne întoarce la tabel
  backToOrders() {
    this.selectedOrder = null;
  }

  // Adaugă metoda asta în clasa ProducerOrdersComponent
 markAsShipped() {
    if (this.selectedOrder) {
      // Salvăm comanda într-o constantă locală ca să nu ne mai dea eroare TypeScript în interiorul subscribe-ului
      const currentOrder = this.selectedOrder;

      this.checkoutService.markOrderAsShipped(currentOrder.id).subscribe({
        next: (updatedOrder) => {
          currentOrder.status = updatedOrder.status;
          
          const index = this.orders.findIndex(o => o.id === currentOrder.id);
          if (index !== -1) {
            this.orders[index].status = updatedOrder.status;
          }
          
          alert('Success! The order was marked as shipped.');
        },
        error: error => console.log(error)
      });
    }
  }
}
