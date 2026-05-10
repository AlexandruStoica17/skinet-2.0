import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs';
import { AccountService } from '../../account/account.service';
import { MessageService } from '../../core/services/message.service';

@Component({
  selector: 'app-conversation',
  templateUrl: './conversation.component.html',
  styleUrls: ['./conversation.component.scss']
})
export class ConversationComponent implements OnInit, OnDestroy {
  recipientEmail = '';
  messageContent = '';
  currentUserEmail = '';
  currentUserToken = '';

  constructor(
    public messageService: MessageService,
    private accountService: AccountService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (user) {
          this.currentUserEmail = user.email;
          this.currentUserToken = user.token;

          // Citim emailul partenerului din query params (?user=email@...)
          this.route.queryParams.pipe(take(1)).subscribe(params => {
            if (params['user']) {
              this.recipientEmail = params['user'];
              this.messageService.createHubConnection(this.currentUserToken, this.recipientEmail);
            } else {
              // Daca nu avem partener, ne intoarcem la inbox
              this.router.navigate(['/chat']);
            }
          });
        }
      }
    });
  }

  sendMessage() {
    if (this.messageContent.trim().length === 0) return;

    this.messageService.sendMessage(this.recipientEmail, this.messageContent).then(() => {
      this.messageContent = '';
    });
  }

  goBack() {
    this.router.navigate(['/chat']);
  }

  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
  }
}