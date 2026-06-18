import { DEFAULT_CURRENCY_CODE, LOCALE_ID, NgModule } from '@angular/core';
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
import { CommonModule, registerLocaleData } from '@angular/common';
import localeRo from '@angular/common/locales/ro';
import { RouterModule } from '@angular/router'; // NOU: necesar pentru routerLink in ProducerOrdersComponent
import { SharedModule } from './shared/shared.module';
import { SellerProfileComponent } from './seller-profile/seller-profile.component';

registerLocaleData(localeRo);

@NgModule({
  declarations: [
    AppComponent,
    AdminComponent,
    AddProductComponent,
    MyProductsComponent,
    EditProductComponent,
    ProducerOrdersComponent,
    SellerProfileComponent,
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
    SharedModule,
  ],
  providers: [
    { provide: LOCALE_ID, useValue: 'ro-RO' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'RON' },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
