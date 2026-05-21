import { Component, OnInit } from '@angular/core';
import { take } from 'rxjs';
import { AccountService } from '../account/account.service';
import { BlogService } from '../core/services/blog.service';
import { Post } from '../shared/models/post';

@Component({
  selector: 'app-blog',
  templateUrl: './blog.component.html',
  styleUrls: ['./blog.component.scss']
})
export class BlogComponent implements OnInit {
  posts: Post[] = [];
  loading = false;
  isBlogger = false;

  constructor(
    private blogService: BlogService,
    private accountService: AccountService
  ) { }

  ngOnInit(): void {
    this.getPosts();
    this.checkBloggerRole();
  }

  getPosts() {
    this.loading = true;

    this.blogService.getPosts().subscribe({
      next: response => {
        this.posts = response;
        this.loading = false;
      },
      error: error => {
        console.log(error);
        this.loading = false;
      }
    });
  }

  checkBloggerRole() {
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      const role = user?.role;

      if (Array.isArray(role)) {
        this.isBlogger = role.includes('Blogger');
      } else {
        this.isBlogger = role === 'Blogger';
      }
    });
  }

  getExcerpt(post: Post): string {
    const source = post.summary || post.content || '';

    if (source.length <= 180) {
      return source;
    }

    return source.substring(0, 180).trim() + '...';
  }
}