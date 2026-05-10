import { Component, OnInit } from '@angular/core';
import { BasketService } from './basket/basket.service';
import { AccountService } from './account/account.service';
import { MessageService } from './core/services/message.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'Skinet';

  constructor(
    private basketService: BasketService,
    private accountService: AccountService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadBasket();
    this.loadCurrentUser();
    this.initConnections();
  }

  loadBasket() {
    const basketId = localStorage.getItem('basket_id');
    if (basketId) this.basketService.getBasket(basketId);
  }

  loadCurrentUser() {
    const token = localStorage.getItem('token');
    this.accountService.loadCurrentUser(token).subscribe();
  }

  initConnections() {
    this.accountService.currentUser$.subscribe(user => {
      if (user?.token) {
        this.messageService.createNotificationConnection(user.token);
        this.messageService.createPresenceConnection(user.token);
      } else {
        this.messageService.stopNotificationConnection();
        this.messageService.stopPresenceConnection();
      }
    });
  }
}