import { Component, OnInit } from '@angular/core';
import { Post } from '../shared/models/post';
import { BlogService } from '../core/services/blog.service';

@Component({
  selector: 'app-blog',
  templateUrl: './blog.component.html',
  styleUrls: ['./blog.component.scss']
})
export class BlogComponent implements OnInit {
  posts: Post[] = [];

  constructor(private blogService: BlogService) { }

  ngOnInit(): void {
    this.getPosts();
  }

  getPosts() {
    this.blogService.getPosts().subscribe({
      next: response => this.posts = response,
      error: error => console.log(error)
    });
  }
}