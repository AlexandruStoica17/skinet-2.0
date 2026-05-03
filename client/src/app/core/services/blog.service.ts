import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Post } from 'src/app/shared/models/post'; // <--- IMPORTUL MAGIC

@Injectable({
  providedIn: 'root'
})
export class BlogService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getPosts() {
    return this.http.get<Post[]>(this.baseUrl + 'blog'); // Folosim 'Post' în loc de BlogPost
  }

  getPost(id: number) {
    return this.http.get<Post>(this.baseUrl + 'blog/' + id); // Folosim 'Post'
  }

  // Adaugă această metodă nouă
  getComments(postId: number) {
    return this.http.get<any[]>(this.baseUrl + 'blog/' + postId + '/comments');
  }

  // Modifică metoda addComment ca să arate așa
  addComment(commentDto: any) {
    return this.http.post(this.baseUrl + 'blog/comments', commentDto);
  }
}