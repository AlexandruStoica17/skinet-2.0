import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, take } from 'rxjs';
import { Message } from 'src/app/shared/models/message';


@Injectable({
  providedIn: 'root'
})
export class MessageService {
  hubUrl = environment.hubUrl;
  private hubConnection?: HubConnection;
  
  // Aici vom stoca lista de mesaje (istoricul) ca să o putem afișa în HTML
  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();

  constructor() { }

  createHubConnection(token: string, otherUsername: string) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'message?user=' + otherUsername, {
        accessTokenFactory: () => token // Trimitem token-ul de autentificare
      })
      .withAutomaticReconnect() // Dacă pică netul, încearcă să se reconecteze
      .build();

    this.hubConnection.start().catch(error => console.log(error));

    // 1. Când ne conectăm, primim tot istoricul de la server
    this.hubConnection.on('ReceiveMessageThread', messages => {
      this.messageThreadSource.next(messages);
    });

    // 2. Când primim un mesaj NOU, îl adăugăm la lista existentă
    this.hubConnection.on('NewMessage', message => {
      this.messageThread$.pipe(take(1)).subscribe({
        next: messages => {
          this.messageThreadSource.next([...messages, message]);
        }
      })
    });
  }

  stopHubConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  // Metoda de trimitere a unui mesaj nou
  async sendMessage(username: string, content: string) {
    return this.hubConnection?.invoke('SendMessage', { recipientUsername: username, content })
      .catch(error => console.log(error));
  }
}