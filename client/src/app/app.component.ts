import { Component, OnInit } from '@angular/core';
import { BasketService } from './basket/basket.service';
import { AccountService } from './account/account.service';
import { MessageService } from './core/services/message.service';
import { filter, switchMap, take } from 'rxjs';

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
    private messageService: MessageService  // NOU
  ) {}

  ngOnInit(): void {
    this.loadBasket();
    this.loadCurrentUser();
    this.initNotifications(); // NOU
  }

  loadBasket() {
    const basketId = localStorage.getItem('basket_id');
    if (basketId) this.basketService.getBasket(basketId);
  }

  loadCurrentUser() {
    const token = localStorage.getItem('token');
    this.accountService.loadCurrentUser(token).subscribe();
  }

  /**
   * Pornim conexiunea de notificări imediat ce avem un user logat.
   * Se oprește automat când userul dă logout (token devine null).
   */
  initNotifications() {
    this.accountService.currentUser$.subscribe(user => {
      if (user?.token) {
        // User tocmai s-a logat — pornim notificările
        this.messageService.createNotificationConnection(user.token);
      } else {
        // User tocmai s-a delogat — oprim notificările
        this.messageService.stopNotificationConnection();
      }
    });
  }
}