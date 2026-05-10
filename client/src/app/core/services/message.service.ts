import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, map, take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
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
  private presenceConnection?: HubConnection;

  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();

  private unreadCountSource = new BehaviorSubject<number>(0);
  unreadCount$ = this.unreadCountSource.asObservable();

  constructor(
    private http: HttpClient,
    private toastr: ToastrService,
    private router: Router
  ) { }

  // ─── REST ──────────────────────────────────────────────────────────────────

  // NOU: inbox cu search + paginare, returneaza { conversations, totalCount }
  getInbox(search = '', pageIndex = 1, pageSize = 10) {
    let params = new HttpParams()
      .set('search', search)
      .set('pageIndex', pageIndex.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<Conversation[]>(this.baseUrl + 'messages/inbox', {
      params,
      observe: 'response'
    }).pipe(
      map((response: HttpResponse<Conversation[]>) => ({
        conversations: response.body ?? [],
        totalCount: parseInt(response.headers.get('X-Pagination-Total') ?? '0', 10)
      }))
    );
  }

  // NOU: cauta useri pentru a incepe o conversatie noua
  searchUsers(query: string) {
    return this.http.get<{ email: string; displayName: string }[]>(
      this.baseUrl + 'messages/search-user',
      { params: new HttpParams().set('query', query) }
    );
  }

  // NOU: trimite review
  submitReview(reviewData: { orderId: number; producerEmail: string; rating: number; comment: string }) {
    return this.http.post(this.baseUrl + 'messages/review', reviewData);
  }

  // ─── SignalR: Notification Hub ─────────────────────────────────────────────

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
  }

  stopNotificationConnection() {
    if (this.notificationConnection) {
      this.notificationConnection.stop();
      this.notificationConnection = undefined;
    }
  }

  // ─── SignalR: Presence Hub ─────────────────────────────────────────────────

  createPresenceConnection(token: string) {
    if (this.presenceConnection) return;

    this.presenceConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'presence', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.presenceConnection.start().catch(error => console.log(error));

    // Toast cu click pentru a deschide conversatia - exact ca in DatingApp
    this.presenceConnection.on('NewMessageReceived', ({ senderEmail, senderName }) => {
      this.toastr.info(senderName + ' ti-a trimis un mesaj nou. Click pentru a vedea.')
        .onTap
        .pipe(take(1))
        .subscribe(() => {
          this.router.navigate(['/chat', 'conversation'], {
            queryParams: { user: senderEmail }
          });
        });

      this.unreadCount$.pipe(take(1)).subscribe(count => {
        this.unreadCountSource.next(count + 1);
      });
    });
  }

  stopPresenceConnection() {
    if (this.presenceConnection) {
      this.presenceConnection.stop();
      this.presenceConnection = undefined;
    }
  }

  // ─── SignalR: Message Hub ──────────────────────────────────────────────────

 // Modifici createHubConnection sa accepte orderId optional:
createHubConnection(token: string, otherUsername: string, orderId?: number) {
    // NOU: adaugam orderId in URL daca exista
    let url = this.hubUrl + 'message?user=' + otherUsername;
    if (orderId) url += '&orderId=' + orderId;

    this.hubConnection = new HubConnectionBuilder()
        .withUrl(url, { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .build();

    this.hubConnection.start().catch(error => console.log(error));

    this.hubConnection.on('ReceiveMessageThread', messages => {
      this.messageThreadSource.next(messages);
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

  // Modifici sendMessage sa trimita orderId:
async sendMessage(username: string, content: string, orderId?: number) {
    return this.hubConnection?.invoke('SendMessage', {
        recipientUsername: username,
        content,
        orderId: orderId ?? null  // NOU
    }).catch(error => console.log(error));
}
}