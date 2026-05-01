import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LikesService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // 1. Schimbăm ruta către 'favorites' și trimitem corpul (DTO-ul) exact cum îl vrea C#
  addLike(productId: number, appUserId: string) {
    return this.http.post(this.baseUrl + 'favorites', { 
      productId: productId, 
      appUserId: appUserId 
    });
  }

  // C#-ul știe deja cine ești din Token, deci nu mai trebuie să îi trimitem appUserId aici
  getLikes() {
    return this.http.get(this.baseUrl + 'favorites');
  }

  // Adaugă asta sub metoda addLike
  removeLike(productId: number, appUserId: string) {
    return this.http.delete(this.baseUrl + 'favorites', {
      body: { 
        productId: productId, 
        appUserId: appUserId 
      }
    });
  }
}