import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from '../core/services/message.service';
import { Conversation } from '../shared/models/conversation';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {
  conversations: Conversation[] = [];
  loading = false;

  constructor(
    public messageService: MessageService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadInbox();
  }

  loadInbox() {
    this.loading = true;
    this.messageService.getInbox().subscribe({
      next: conversations => {
        this.conversations = conversations;
        this.loading = false;
      },
      error: error => {
        console.log(error);
        this.loading = false;
      }
    });
  }

  openConversation(partnerEmail: string) {
    this.router.navigate(['/chat', 'conversation'], {
      queryParams: { user: partnerEmail }
    });
  }
}