import { Component, OnInit } from '@angular/core';
import { take } from 'rxjs';
import { Post } from '../shared/models/post';
import { BlogService } from '../core/services/blog.service';
import { AccountService } from '../account/account.service';

@Component({
  selector: 'app-blog',
  templateUrl: './blog.component.html',
  styleUrls: ['./blog.component.scss']
})
export class BlogComponent implements OnInit {
  posts: Post[] = [];
  isBlogger = false;

  constructor(
    private blogService: BlogService,
    private accountService: AccountService
  ) { }

  ngOnInit(): void {
    this.getPosts();

    // MODIFICAT: doar bloggerii văd butonul de creare articol
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.isBlogger = !!user?.role?.includes('Blogger');
    });
  }

  getPosts() {
    this.blogService.getPosts().subscribe({
      next: response => this.posts = response,
      error: error => console.log(error)
    });
  }
}