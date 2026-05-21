import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { BlogService } from 'src/app/core/services/blog.service';
import { Post } from 'src/app/shared/models/post';

@Component({
  selector: 'app-my-posts',
  templateUrl: './my-posts.component.html',
  styleUrls: ['./my-posts.component.scss']
})
export class MyPostsComponent implements OnInit {
  posts: Post[] = [];
  loading = false;

  constructor(
    private blogService: BlogService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.getMyPosts();
  }

  getMyPosts() {
    this.loading = true;

    this.blogService.getMyPosts().subscribe({
      next: response => {
        this.posts = response;
        this.loading = false;
      },
      error: error => {
        console.log(error);
        this.loading = false;
        this.toastr.error('Nu s-au putut încărca articolele tale.');
      }
    });
  }

  deletePost(id: number) {
    if (!confirm('Sigur vrei să ștergi acest articol?')) return;

    this.blogService.deletePost(id).subscribe({
      next: () => {
        this.toastr.success('Articolul a fost șters.');
        this.posts = this.posts.filter(p => p.id !== id);
      },
      error: error => {
        console.log(error);
        this.toastr.error('Nu s-a putut șterge articolul.');
      }
    });
  }

  getExcerpt(post: Post): string {
    const source = post.content || '';

    if (source.length <= 140) {
      return source;
    }

    return source.substring(0, 140).trim() + '...';
  }
}