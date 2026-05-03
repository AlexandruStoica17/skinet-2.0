import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BlogService } from 'src/app/core/services/blog.service';
import { Post } from 'src/app/shared/models/post';
import { AccountService } from 'src/app/account/account.service'; // Pentru a verifica logarea

@Component({
  selector: 'app-post-details',
  templateUrl: './post-details.component.html',
  styleUrls: ['./post-details.component.scss']
})
export class PostDetailsComponent implements OnInit {
  post?: Post;
  comments: any[] = []; // Lista de comentarii
  newCommentContent = ''; // Textul din formular

  constructor(
    private blogService: BlogService,
    private route: ActivatedRoute,
    public accountService: AccountService // public ca să-l folosim în HTML
  ) { }

  ngOnInit(): void {
    this.loadPost();
  }

  loadPost() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.blogService.getPost(+id).subscribe({
        next: post => {
          this.post = post;
          this.loadComments(post.id); // Încărcăm comentariile după ce avem postarea
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
      // Dacă backend-ul tău cere și AppUserId aici, îl vom adăuga. 
      // De obicei, e preluat din Token în C#.
    };

    this.blogService.addComment(commentDto).subscribe({
      next: (comment) => {
        // Adăugăm comentariul nou direct în listă fără să dăm refresh
        this.comments.push(comment);
        this.newCommentContent = ''; // Golim câmpul de text
      },
      error: error => console.log(error)
    });
  }
}