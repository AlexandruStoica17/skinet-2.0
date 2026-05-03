import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BlogComponent } from './blog.component';
import { PostDetailsComponent } from './post-details/post-details.component'; // <--- Import nou

const routes: Routes = [
  { path: '', component: BlogComponent },
  { path: ':id', component: PostDetailsComponent } // <--- Rută nouă pentru un articol specific
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class BlogRoutingModule { }