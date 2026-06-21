import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs';
import { AccountService } from '../../account/account.service';
import { MessageService } from '../../core/services/message.service';
import { OrdersService } from '../../orders/orders.service';
import { ReviewService } from '../../core/services/review.service';
import { OrderItem } from '../../shared/models/order';
import { Message } from '../../shared/models/message';

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
  orderId?: number;
  isSelfConversation = false;

  deliveredMarked = false;
  showDeliveryButton = false;
  confirmingDelivery = false;
  private orderStatus = '';
  private ensuringDeliveryPrompt = false;
  private deliveryPromptEnsureAttempted = false;

  showReviewForm = false;
  reviewOrderId: number | null = null;
  sellerRating = 5;
  sellerComment = '';

  orderItems: OrderItem[] = [];
  productRatings: { [productId: number]: number } = {};
  productComments: { [productId: number]: string } = {};
  productReviewsDone: { [productId: number]: boolean } = {};

  reviewSubmitting = false;
  reviewSubmitted = false;

  constructor(
    public messageService: MessageService,
    private accountService: AccountService,
    private route: ActivatedRoute,
    private router: Router,
    private ordersService: OrdersService,
    private reviewService: ReviewService
  ) { }

  ngOnInit(): void {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (!user) return;

        this.currentUserEmail = user.email;
        this.currentUserToken = user.token;

        this.route.queryParams.pipe(take(1)).subscribe(params => {
          if (!params['user']) {
            this.router.navigate(['/chat']);
            return;
          }

          this.recipientEmail = params['user'].trim();
          if (params['orderId']) this.orderId = +params['orderId'];

          this.isSelfConversation =
            this.recipientEmail.toLowerCase() === this.currentUserEmail.toLowerCase();

          if (this.isSelfConversation) {
            this.messageService.stopHubConnection();
            return;
          }

          this.messageService.createHubConnection(
            this.currentUserToken, this.recipientEmail, this.orderId
          );

          this.messageService.messageThread$.subscribe(messages => {
            this.updateDeliveryButton(messages);
          });

          if (this.orderId) {
            this.ordersService.getOrderDetailed(this.orderId).subscribe({
              next: order => {
                this.orderItems = order.orderItems;
                this.orderStatus = order.status;

                if (order.status === 'Delivered') {
                  this.deliveredMarked = true;
                  this.showDeliveryButton = false;
                }

                this.messageService.messageThread$.pipe(take(1)).subscribe(messages => {
                  this.updateDeliveryButton(messages);
                });

                order.orderItems.forEach(item => {
                  this.productRatings[item.productId] = 5;
                  this.productComments[item.productId] = '';
                  this.productReviewsDone[item.productId] = false;
                });
              }
            });
          }
        });
      }
    });
  }

  async sendMessage() {
    if (this.messageContent.trim().length === 0) return;
    const sent = await this.messageService.sendMessage(this.recipientEmail, this.messageContent, this.orderId);
    if (sent) this.messageContent = '';
  }

  private updateDeliveryButton(messages: Message[]) {
    const alreadyDelivered = messages.some(m =>
      m.content.includes('confirmed delivery') || m.isReviewPrompt
    );

    if (alreadyDelivered) {
      this.deliveredMarked = true;
      this.showDeliveryButton = false;
      return;
    }

    if (this.orderStatus === 'Delivered') {
      this.deliveredMarked = true;
      this.showDeliveryButton = false;
      this.ensureDeliveryPrompt();
      return;
    }

    const shippedMsg = messages.find(m => m.content.includes('has been shipped'));
    const isBuyer = shippedMsg && shippedMsg.recipientUsername === this.currentUserEmail;
    this.showDeliveryButton = !!shippedMsg && !!isBuyer && !this.confirmingDelivery;
  }

  private ensureDeliveryPrompt() {
    if (!this.orderId || this.ensuringDeliveryPrompt || this.deliveryPromptEnsureAttempted) return;

    this.deliveryPromptEnsureAttempted = true;
    this.ensuringDeliveryPrompt = true;
    this.ordersService.markOrderAsDelivered(this.orderId).subscribe({
      next: () => {
        this.ensuringDeliveryPrompt = false;
        this.refreshMessageThread();
      },
      error: err => {
        this.ensuringDeliveryPrompt = false;
        console.log(err);
      }
    });
  }

  private refreshMessageThread() {
    this.messageService.stopHubConnection();
    setTimeout(() => {
      this.messageService.createHubConnection(this.currentUserToken, this.recipientEmail, this.orderId);
    }, 200);
  }

  markDelivered(orderId: number) {
    if (this.deliveredMarked || this.confirmingDelivery) return;

    this.confirmingDelivery = true;
    this.ordersService.markOrderAsDelivered(orderId).subscribe({
      next: order => {
        this.orderStatus = order.status;
        this.deliveredMarked = true;
        this.showDeliveryButton = false;
        this.confirmingDelivery = false;
        this.deliveryPromptEnsureAttempted = true;

        this.refreshMessageThread();
      },
      error: err => {
        this.confirmingDelivery = false;
        if (err.status === 400) {
          this.deliveredMarked = true;
          this.showDeliveryButton = false;
        }
        console.log(err);
      }
    });
  }

  openReviewForm(orderId: number) {
    this.reviewOrderId = orderId;
    this.showReviewForm = true;
    this.reviewSubmitted = false;
  }

  submitAllReviews() {
    if (!this.reviewOrderId) return;
    this.reviewSubmitting = true;

    this.messageService.submitReview({
      orderId: this.reviewOrderId,
      producerEmail: this.recipientEmail,
      rating: this.sellerRating,
      comment: this.sellerComment
    }).subscribe({
      next: () => {
        const productReviewCalls = this.orderItems.map(item =>
          this.reviewService.submitProductReview({
            productId: item.productId,
            orderId: this.reviewOrderId!,
            rating: this.productRatings[item.productId] ?? 5,
            comment: this.productComments[item.productId] ?? ''
          })
        );

        let completed = 0;
        if (productReviewCalls.length === 0) {
          this.reviewSubmitting = false;
          this.reviewSubmitted = true;
          this.showReviewForm = false;
          return;
        }

        productReviewCalls.forEach(call => {
          call.subscribe({
            next: () => {
              completed++;
              if (completed === productReviewCalls.length) {
                this.reviewSubmitting = false;
                this.reviewSubmitted = true;
                this.showReviewForm = false;
              }
            },
            error: () => {
              completed++;
              if (completed === productReviewCalls.length) {
                this.reviewSubmitting = false;
                this.reviewSubmitted = true;
                this.showReviewForm = false;
              }
            }
          });
        });
      },
      error: err => {
        console.log(err);
        this.reviewSubmitting = false;
      }
    });
  }

  goBack() { this.router.navigate(['/chat']); }
  ngOnDestroy(): void { this.messageService.stopHubConnection(); }
}
