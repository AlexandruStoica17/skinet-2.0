import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChatComponent } from './chat.component';

const routes: Routes = [
  // Aici e cheia: path-ul trebuie să fie gol (''), pentru că ruta principală 'chat' e deja definită în app-routing
  { path: '', component: ChatComponent } 
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ChatRoutingModule { }