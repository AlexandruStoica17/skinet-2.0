import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BlogComponent } from './blog.component';
import { PostDetailsComponent } from './post-details/post-details.component';
import { CreatePostComponent } from './create-post/create-post.component';
import { MyPostsComponent } from './my-posts/my-posts.component';
import { EditPostComponent } from './edit-post/edit-post.component';
import { AuthGuard } from '../core/guards/auth.guard';

const routes: Routes = [
  {
    path: '',
    component: BlogComponent
  },
  {
    path: 'create',
    component: CreatePostComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'my-posts',
    component: MyPostsComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'edit/:id',
    component: EditPostComponent,
    canActivate: [AuthGuard]
  },
  {
    path: ':id',
    component: PostDetailsComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class BlogRoutingModule { }