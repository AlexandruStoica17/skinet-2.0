import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs';
import { AccountService } from '../../account/account.service';
import { MessageService } from '../../core/services/message.service';
import { OrdersService } from '../../orders/orders.service';

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

  // NOU: orderId din URL
  orderId?: number;

  // NOU: review form
  showReviewForm = false;
  reviewOrderId: number | null = null;
  reviewRating = 5;
  reviewComment = '';
  reviewSubmitting = false;
  reviewSubmitted = false;

  constructor(
    public messageService: MessageService,
    private accountService: AccountService,
    private route: ActivatedRoute,
    private router: Router,
    private ordersService: OrdersService // NOU
  ) { }

  ngOnInit(): void {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (user) {
          this.currentUserEmail = user.email;
          this.currentUserToken = user.token;

          this.route.queryParams.pipe(take(1)).subscribe(params => {
            if (!params['user']) {
              this.router.navigate(['/chat']);
              return;
            }

            this.recipientEmail = params['user'];

            // NOU: citim orderId din URL (?orderId=11)
            if (params['orderId']) {
              this.orderId = +params['orderId'];
            }

            // NOU: trimitem orderId la hub
            this.messageService.createHubConnection(
              this.currentUserToken,
              this.recipientEmail,
              this.orderId
            );
          });
        }
      }
    });
  }

  sendMessage() {
    if (this.messageContent.trim().length === 0) return;
    // NOU: trimitem si orderId
    this.messageService.sendMessage(this.recipientEmail, this.messageContent, this.orderId)
      .then(() => { this.messageContent = ''; });
  }

  // NOU: cumparatorul confirma primirea comenzii
  markDelivered(orderId: number) {
    this.ordersService.markOrderAsDelivered(orderId).subscribe({
      next: () => {
        // Mesajele automate vor aparea prin SignalR
      },
      error: err => console.log(err)
    });
  }

  // NOU: deschide formularul de review
  openReviewForm(orderId: number) {
    this.reviewOrderId = orderId;
    this.showReviewForm = true;
    this.reviewSubmitted = false;
  }

  // NOU: trimite review-ul
  submitReview() {
    if (!this.reviewOrderId) return;
    this.reviewSubmitting = true;

    this.messageService.submitReview({
      orderId: this.reviewOrderId,
      producerEmail: this.recipientEmail,
      rating: this.reviewRating,
      comment: this.reviewComment
    }).subscribe({
      next: () => {
        this.reviewSubmitting = false;
        this.reviewSubmitted = true;
        this.showReviewForm = false;
      },
      error: err => {
        console.log(err);
        this.reviewSubmitting = false;
      }
    });
  }

  goBack() {
    this.router.navigate(['/chat']);
  }

  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
  }
}