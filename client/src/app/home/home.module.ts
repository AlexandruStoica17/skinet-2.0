import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { HomeComponent } from './home.component';
import { CarouselModule } from 'ngx-bootstrap/carousel';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    HomeComponent
  ],
  imports: [
    CommonModule,
    RouterModule, // NOU: necesar pentru routerLink și queryParams în home.component.html
    CarouselModule,
    SharedModule
  ],
  exports: [
    HomeComponent
  ]
})
export class HomeModule { }