import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CoreModule } from './core/core.module';
import { ShopModule } from './shop/shop.module';
import { HomeComponent } from './home/home.component';
import { HomeModule } from './home/home.module';
import { ErrorInterceptor } from './core/interceptors/error.interceptor';
import { BreadcrumbModule } from 'xng-breadcrumb';
import { LoadingInterceptor } from './core/interceptors/loading.interceptor';
import { JwtInterceptor } from './core/interceptors/jwt.interceptor';
import { OrderDetailedComponent } from './order-detailed/order-detailed.component';
import { OrdersModule } from './orders/orders.module';
import { AdminComponent } from './admin/admin.component';
import { AddProductComponent } from './producer/add-product/add-product.component';
import { ReactiveFormsModule } from '@angular/forms';
import { MyProductsComponent } from './producer/my-products/my-products.component';
import { EditProductComponent } from './producer/edit-product/edit-product.component';
import { ProducerOrdersComponent } from './producer/producer-orders/producer-orders.component';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    AppComponent,
    AdminComponent,
    AddProductComponent,
    MyProductsComponent,
    EditProductComponent,
    ProducerOrdersComponent,
    // OrderDetailedComponent
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
    // CommonModule,
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
