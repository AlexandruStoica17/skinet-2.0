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

  // NOU: search + paginare
  searchTerm = '';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;

  // NOU: new conversation search
  showNewConversation = false;
  userSearchQuery = '';
  userSearchResults: { email: string; displayName: string }[] = [];
  searchingUsers = false;

  constructor(
    public messageService: MessageService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadInbox();
  }

  loadInbox() {
    this.loading = true;
    this.messageService.getInbox(this.searchTerm, this.pageNumber, this.pageSize).subscribe({
      next: ({ conversations, totalCount }) => {
        this.conversations = conversations;
        this.totalCount = totalCount;
        this.loading = false;
      },
      error: error => {
        console.log(error);
        this.loading = false;
      }
    });
  }

  // NOU: se apeleaza cand userul scrie in search bar
  onSearch() {
    this.pageNumber = 1; // resetam la prima pagina la search nou
    this.loadInbox();
  }

  // NOU: se apeleaza de pager
  onPageChanged(page: number) {
    this.pageNumber = page;
    this.loadInbox();
  }

  openConversation(partnerEmail: string, orderId?: number) {
    const queryParams: any = { user: partnerEmail };
    // NOU: daca e o conversatie de comanda, trimitem si orderId
    if (orderId) queryParams['orderId'] = orderId;

    this.router.navigate(['/chat', 'conversation'], { queryParams });
}

  // NOU: cauta useri pentru conversatie noua
  searchUsers() {
    if (this.userSearchQuery.length < 2) {
      this.userSearchResults = [];
      return;
    }
    this.searchingUsers = true;
    this.messageService.searchUsers(this.userSearchQuery).subscribe({
      next: results => {
        this.userSearchResults = results;
        this.searchingUsers = false;
      }
    });
  }

  // NOU: incepe conversatie noua cu userul selectat din cautare
  startNewConversation(email: string) {
    this.showNewConversation = false;
    this.userSearchQuery = '';
    this.userSearchResults = [];
    this.openConversation(email);
  }

  
}