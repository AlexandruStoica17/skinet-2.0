import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { NewsComponent } from './news.component';
import { SharedModule } from '../shared/shared.module';

// Internal route for this lazy-loaded module
const routes: Routes = [
  { path: '', component: NewsComponent }
];

@NgModule({
  declarations: [NewsComponent],
  imports: [
    CommonModule,
    SharedModule,
    RouterModule.forChild(routes),  // lazy-loaded child routes
  ]
})
export class NewsModule { }
