import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ChatRoutingModule } from './chat-routing.module';
import { ChatComponent } from './chat.component';
import { ConversationComponent } from './conversation/conversation.component';
import { SharedModule } from '../shared/shared.module'; // NOU: pentru app-pager
import { OrdersService } from '../orders/orders.service';

@NgModule({
  declarations: [
    ChatComponent,
    ConversationComponent
  ],
  imports: [
    CommonModule,
    ChatRoutingModule,
    FormsModule,
    RouterModule,
    SharedModule,
   
  ]
})
export class ChatModule { }