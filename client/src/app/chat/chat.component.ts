import { Component, OnDestroy, OnInit } from '@angular/core';


import { take } from 'rxjs';
import { AccountService } from '../account/account.service';
import { MessageService } from '../core/services/message.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy {
  recipientUsername = '';
  messageContent = '';
  currentUserToken = '';
  currentUsername = '';
  connected = false;

  constructor(
    public messageService: MessageService, 
    private accountService: AccountService
  ) { }

  ngOnInit(): void {
    // Extragem token-ul și numele utilizatorului curent logat
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (user) {
          this.currentUserToken = user.token;
          // În API-ul tău s-ar putea ca Username-ul să fie de fapt adresa de email
          this.currentUsername = user.email; 
        }
      }
    });
  }

  // Pornim conexiunea de SignalR
  connectToChat() {
    if (this.recipientUsername && this.currentUserToken) {
      this.messageService.createHubConnection(this.currentUserToken, this.recipientUsername);
      this.connected = true;
    }
  }

  // Trimitem mesajul
  sendMessage() {
    if (this.messageContent.trim().length === 0) return;

    this.messageService.sendMessage(this.recipientUsername, this.messageContent).then(() => {
      this.messageContent = ''; // Curățăm căsuța după trimitere
    });
  }

  // Închidem conexiunea când ieșim de pe pagină
  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
  }
}