import { Component, OnInit } from '@angular/core';
import { BreadcrumbService } from 'xng-breadcrumb';
import { ActivatedRoute } from '@angular/router';
import { BlogService } from 'src/app/core/services/blog.service';
import { Post } from 'src/app/shared/models/post';
import { AccountService } from 'src/app/account/account.service';
import { Product } from 'src/app/shared/models/product';   // NEW
import { ShopService } from 'src/app/shop/shop.service';   // NEW

@Component({
  selector: 'app-post-details',
  templateUrl: './post-details.component.html',
  styleUrls: ['./post-details.component.scss']
})
export class PostDetailsComponent implements OnInit {
  post?: Post;
  comments: any[] = [];
  newCommentContent = '';

  // NEW: products suggested from blog post content keywords
  suggestedProducts: Product[] = [];

  constructor(
  private blogService: BlogService,
  private route: ActivatedRoute,
  public accountService: AccountService,
  private shopService: ShopService,
  private bcService: BreadcrumbService
) { 
  this.bcService.set('@postDetails', ' ');
}

  ngOnInit(): void {
    this.loadPost();
  }

  loadPost() {
  const id = this.route.snapshot.paramMap.get('id');

  if (id) {
    this.blogService.getPost(+id).subscribe({
      next: post => {
        this.post = post;

        // NOU: în breadcrumb apare titlul articolului, nu id-ul
        this.bcService.set('@postDetails', post.title);

        this.loadComments(post.id);

        const text = (post.title || '') + ' ' + (post.content || '') + ' ' + (post.summary || '');
        const keywords = this.shopService.extractKeywords(text);

        if (keywords) {
          this.shopService.getSuggestions(keywords, 0, 4).subscribe({
            next: suggestions => this.suggestedProducts = suggestions,
            error: err => console.log(err)
          });
        }
      },
      error: error => console.log(error)
    });
  }
}

  loadComments(postId: number) {
    this.blogService.getComments(postId).subscribe({
      next: comments => this.comments = comments,
      error: error => console.log(error)
    });
  }

  addComment() {
    if (!this.post || !this.newCommentContent.trim()) return;

    const commentDto = {
      postId: this.post.id,
      content: this.newCommentContent
    };

    this.blogService.addComment(commentDto).subscribe({
      next: comment => {
        this.comments.push(comment);
        this.newCommentContent = '';
      },
      error: error => console.log(error)
    });
  }
}