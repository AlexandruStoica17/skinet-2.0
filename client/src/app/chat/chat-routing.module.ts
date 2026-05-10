import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChatComponent } from './chat.component';
import { ConversationComponent } from './conversation/conversation.component';

const routes: Routes = [
  { path: '', component: ChatComponent },                          // /chat → inbox
  { path: 'conversation', component: ConversationComponent }      // /chat/conversation?user=... → chat
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChatRoutingModule { }