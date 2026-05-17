import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CoreModule } from './core/core.module';
import { HomeModule } from './home/home.module';
import { ErrorInterceptor } from './core/interceptors/error.interceptor';
import { BreadcrumbModule } from 'xng-breadcrumb';
import { LoadingInterceptor } from './core/interceptors/loading.interceptor';
import { JwtInterceptor } from './core/interceptors/jwt.interceptor';
import { OrdersModule } from './orders/orders.module';
import { AdminComponent } from './admin/admin.component';
import { AddProductComponent } from './producer/add-product/add-product.component';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MyProductsComponent } from './producer/my-products/my-products.component';
import { EditProductComponent } from './producer/edit-product/edit-product.component';
import { ProducerOrdersComponent } from './producer/producer-orders/producer-orders.component';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // NOU: necesar pentru routerLink in ProducerOrdersComponent

@NgModule({
  declarations: [
    AppComponent,
    AdminComponent,
    AddProductComponent,
    MyProductsComponent,
    EditProductComponent,
    ProducerOrdersComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    HttpClientModule,
    CoreModule,
    HomeModule,
    BreadcrumbModule,
    OrdersModule,
    ReactiveFormsModule,
    FormsModule,       // NOU: necesar pentru [(ngModel)] in producer components
    CommonModule,      // FIX: decomentam — necesar pentru *ngIf, *ngFor, pipes
    RouterModule,      // FIX: necesar pentru routerLink, queryParams in ProducerOrdersComponent
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}