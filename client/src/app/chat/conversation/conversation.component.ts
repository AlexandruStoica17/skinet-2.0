import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs';
import { AccountService } from '../../account/account.service';
import { MessageService } from '../../core/services/message.service';
import { OrdersService } from '../../orders/orders.service';
import { ReviewService } from '../../core/services/review.service';
import { OrderItem } from '../../shared/models/order';

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

  deliveredMarked = false;
  showDeliveryButton = false; // Folosit pentru bara de actiune

  // Review vanzator
  showReviewForm = false;
  reviewOrderId: number | null = null;
  sellerRating = 5;
  sellerComment = '';

  // Reviews produse
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
        if (user) {
          this.currentUserEmail = user.email;
          this.currentUserToken = user.token;

          this.route.queryParams.pipe(take(1)).subscribe(params => {
            if (!params['user']) {
              this.router.navigate(['/chat']);
              return;
            }

            this.recipientEmail = params['user'];
            if (params['orderId']) this.orderId = +params['orderId'];

            this.messageService.createHubConnection(
              this.currentUserToken, this.recipientEmail, this.orderId
            );

            // ---> MODIFICAT AICI: Verificam statusul comenzii si daca afisam bara de confirmare
            this.messageService.messageThread$.subscribe(messages => {
              const alreadyDelivered = messages.some(m =>
                m.content.includes('confirmed delivery') || m.isReviewPrompt
              );
              
              const shippedMsg = messages.find(m => m.content.includes('has been shipped'));
              const isBuyer = shippedMsg && shippedMsg.recipientUsername === this.currentUserEmail;

              if (alreadyDelivered) {
                this.deliveredMarked = true;
                this.showDeliveryButton = false;
              } else if (shippedMsg && isBuyer) {
                this.showDeliveryButton = true;
              }
            });

            // Incarcam produsele comenzii daca avem orderId
            if (this.orderId) {
              this.ordersService.getOrderDetailed(this.orderId).subscribe({
                next: order => {
                  this.orderItems = order.orderItems;
                  // Initializam rating-ul la 5 pentru fiecare produs
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
      }
    });
  }

  sendMessage() {
    if (this.messageContent.trim().length === 0) return;
    this.messageService.sendMessage(this.recipientEmail, this.messageContent, this.orderId)
      .then(() => { this.messageContent = ''; });
  }

  // ---> MODIFICAT AICI: Adaugat refresh instant la SignalR
  markDelivered(orderId: number) {
    if (this.deliveredMarked) return;
    this.ordersService.markOrderAsDelivered(orderId).subscribe({
      next: () => { 
        this.deliveredMarked = true; 
        this.showDeliveryButton = false;

        // Oprim și repornim conexiunea pentru a aduce instant noul mesaj de sistem cu Review
        this.messageService.stopHubConnection();
        setTimeout(() => {
          this.messageService.createHubConnection(this.currentUserToken, this.recipientEmail, this.orderId);
        }, 200); // O scurtă întârziere pentru a ne asigura că vechea conexiune s-a închis
      },
      error: err => {
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

  // Trimite review-ul pentru vanzator + toate produsele dintr-o data
  submitAllReviews() {
    if (!this.reviewOrderId) return;
    this.reviewSubmitting = true;

    // 1. Review vanzator
    this.messageService.submitReview({
      orderId: this.reviewOrderId,
      producerEmail: this.recipientEmail,
      rating: this.sellerRating,
      comment: this.sellerComment
    }).subscribe({
      next: () => {
        // 2. Review pentru fiecare produs
        const productReviewCalls = this.orderItems.map(item =>
          this.reviewService.submitProductReview({
            productId: item.productId,
            orderId: this.reviewOrderId!,
            rating: this.productRatings[item.productId] ?? 5,
            comment: this.productComments[item.productId] ?? ''
          })
        );

        // Trimitem toate review-urile de produs in paralel
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
