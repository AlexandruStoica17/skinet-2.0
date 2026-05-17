import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BlogRoutingModule } from './blog-routing.module';
import { BlogComponent } from './blog.component';
import { PostDetailsComponent } from './post-details/post-details.component';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router'; // NEW: needed for [routerLink] in suggestions

@NgModule({
  declarations: [
    BlogComponent,
    PostDetailsComponent
  ],
  imports: [
    CommonModule,
    BlogRoutingModule,
    FormsModule,
    RouterModule,  // NEW: enables [routerLink] and currency pipe in post-details template
  ]
})
export class BlogModule { }