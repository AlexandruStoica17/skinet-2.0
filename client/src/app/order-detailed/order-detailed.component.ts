import { Component, OnInit } from '@angular/core'; 
import { ActivatedRoute } from '@angular/router'; 
import { Order } from 'src/app/shared/models/order'; 
import { BreadcrumbService } from 'xng-breadcrumb'; 
import { OrdersService } from '../orders/orders.service';

@Component({ 
  selector: 'app-order-detailed', 
  templateUrl: './order-detailed.component.html', 
  styleUrls: ['./order-detailed.component.scss'] 
}) 
export class OrderDetailedComponent implements OnInit { 
  order?: Order; 
 
  constructor(
    private orderService: OrdersService, 
    private route: ActivatedRoute,  
    private bcService: BreadcrumbService
  ) {} 
 
  ngOnInit(): void { 
    const id = this.route.snapshot.paramMap.get('id'); 
    if (id) {
        this.orderService.getOrderDetailed(+id).subscribe({ 
        next: order => { 
            this.order = order; 
            // Setăm titlul breadcrumb-ului pentru navigație (ex: Order# 1 - Pending)
            this.bcService.set('@OrderDetailed', `Order# ${order.id} - ${order.status}`); 
        },
        error: error => console.log(error)
        }); 
    }
  } 
}