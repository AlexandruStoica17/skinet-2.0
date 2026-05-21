import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Post } from 'src/app/shared/models/post';

@Injectable({
  providedIn: 'root'
})
export class BlogService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getPosts() {
    return this.http.get<Post[]>(this.baseUrl + 'blog');
  }

  getPost(id: number) {
    return this.http.get<Post>(this.baseUrl + 'blog/' + id);
  }

  // NOU: blogurile userului logat
  getMyPosts() {
    return this.http.get<Post[]>(this.baseUrl + 'blog/my-posts');
  }

  // NOU: creare articol, cu FormData pentru imagini
  createPost(formData: FormData) {
    return this.http.post<Post>(this.baseUrl + 'blog', formData);
  }

  // NOU: editare articol
  updatePost(id: number, formData: FormData) {
    return this.http.put<Post>(this.baseUrl + 'blog/edit/' + id, formData);
  }

  // NOU: ștergere articol
  deletePost(id: number) {
    return this.http.delete(this.baseUrl + 'blog/delete/' + id);
  }

  getComments(postId: number) {
    return this.http.get<any[]>(this.baseUrl + 'blog/' + postId + '/comments');
  }

  addComment(commentDto: any) {
    return this.http.post(this.baseUrl + 'blog/comments', commentDto);
  }
}