import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { OrdersComponent } from './orders.component';
import { ProducerOrdersComponent } from '../producer/producer-orders/producer-orders.component';
import { OrderDetailedComponent } from '../order-detailed/order-detailed.component';

const routes: Routes = [
  { path: '', component: OrdersComponent },
  // { path: 'producer', component: ProducerOrdersComponent, data: { breadcrumb: 'Comenzile Mele (Vânzător)' } }, // <-- ADAUGĂ RUTA AICI
  { path: ':id', component: OrderDetailedComponent, data: { breadcrumb: { alias: 'OrderDetailed' } } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class OrdersRoutingModule { }