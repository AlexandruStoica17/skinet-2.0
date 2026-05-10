import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, take } from 'rxjs';
import { Message } from 'src/app/shared/models/message';
import { Conversation } from 'src/app/shared/models/conversation';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  baseUrl = environment.apiUrl;
  hubUrl = environment.hubUrl;

  private hubConnection?: HubConnection;
  private notificationConnection?: HubConnection;

  // Stream cu mesajele din conversatia curenta
  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();

  // Stream cu numarul de mesaje necitite (pentru badge navbar)
  private unreadCountSource = new BehaviorSubject<number>(0);
  unreadCount$ = this.unreadCountSource.asObservable();

  constructor(private http: HttpClient) { }

  // ─── REST ────────────────────────────────────────────────────────────────────

  // Aduce lista de conversatii pentru inbox
  getInbox() {
    return this.http.get<Conversation[]>(this.baseUrl + 'messages/inbox');
  }

  // ─── SignalR: Chat (conversatie cu o persoana) ────────────────────────────────

  createHubConnection(token: string, otherUsername: string) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'message?user=' + otherUsername, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch(error => console.log(error));

    this.hubConnection.on('ReceiveMessageThread', messages => {
      this.messageThreadSource.next(messages);
      // Resetam badge-ul cand deschidem o conversatie
      this.unreadCountSource.next(0);
    });

    this.hubConnection.on('NewMessage', message => {
      this.messageThread$.pipe(take(1)).subscribe({
        next: messages => {
          this.messageThreadSource.next([...messages, message]);
        }
      });
    });
  }

  stopHubConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = undefined;
      this.messageThreadSource.next([]);
    }
  }

  async sendMessage(username: string, content: string) {
    return this.hubConnection?.invoke('SendMessage', { recipientUsername: username, content })
      .catch(error => console.log(error));
  }

  // ─── SignalR: Notificari (global, pornit la login) ────────────────────────────

  createNotificationConnection(token: string) {
    if (this.notificationConnection) return;

    this.notificationConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'notification', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.notificationConnection.start().catch(error => console.log(error));

    this.notificationConnection.on('UnreadCount', (count: number) => {
      this.unreadCountSource.next(count);
    });

    this.notificationConnection.on('NewMessageNotification', () => {
      this.unreadCount$.pipe(take(1)).subscribe(count => {
        this.unreadCountSource.next(count + 1);
      });
    });
  }

  stopNotificationConnection() {
    if (this.notificationConnection) {
      this.notificationConnection.stop();
      this.notificationConnection = undefined;
    }
  }
}