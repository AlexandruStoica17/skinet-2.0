import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { BlogRoutingModule } from './blog-routing.module';
import { BlogComponent } from './blog.component';
import { PostDetailsComponent } from './post-details/post-details.component';
import { FormsModule } from '@angular/forms';


@NgModule({
  declarations: [
    BlogComponent,
    PostDetailsComponent
  ],
  imports: [
    CommonModule,
    BlogRoutingModule,
    FormsModule,
  ]
})
export class BlogModule { }
