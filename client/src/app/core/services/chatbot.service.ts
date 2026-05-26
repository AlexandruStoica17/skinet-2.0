import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ChatbotHistoryMessage, ChatbotResponse } from 'src/app/shared/models/chatbot';

@Injectable({
  providedIn: 'root'
})
export class ChatbotService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ask(message: string, history: ChatbotHistoryMessage[]): Observable<ChatbotResponse> {
    return this.http.post<ChatbotResponse>(this.baseUrl + 'chatbot/ask', {
      message,
      history: history.slice(-8)
    });
  }
}
