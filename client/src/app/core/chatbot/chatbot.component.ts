import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ChatbotService } from '../services/chatbot.service';
import { ChatbotMessage } from 'src/app/shared/models/chatbot';

interface ChatbotMessageSegment {
  text: string;
  routerLink?: string;
}

@Component({
  selector: 'app-chatbot',
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.scss']
})
export class ChatbotComponent {
  isOpen = false;
  isLoading = false;
  draft = '';

  readonly quickPrompts = [
    'How do I track my order?',
    'How do I contact a seller?',
    'How do product recommendations work?'
  ];

  messages: ChatbotMessage[] = [
    {
      role: 'assistant',
      content: 'Hi, I am the GreenBeauty assistant. I can help with orders, products, sellers, ingredients and platform questions.',
      isWelcome: true
    }
  ];

  constructor(
    private chatbotService: ChatbotService,
    private router: Router
  ) {}

  toggleChat(): void {
    this.isOpen = !this.isOpen;
  }

  askQuickPrompt(prompt: string): void {
    this.draft = prompt;
    this.sendMessage();
  }

  sendMessage(): void {
    const message = this.draft.trim();

    if (!message || this.isLoading) {
      return;
    }

    const history = this.messages
      .filter(item => !item.isWelcome)
      .map(item => ({ role: item.role, content: item.content }));

    this.messages.push({ role: 'user', content: message });
    this.draft = '';
    this.isLoading = true;

    this.chatbotService.ask(message, history)
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: response => {
          this.messages.push({
            role: 'assistant',
            content: response.answer,
            sources: response.sources,
            mode: response.mode
          });
        },
        error: () => {
          this.messages.push({
            role: 'assistant',
            content: 'I could not answer right now. Please try again in a moment or contact support.'
          });
        }
      });
  }

  trackByIndex(index: number): number {
    return index;
  }

  navigateTo(route: string, event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    if (!route) {
      return;
    }

    this.router.navigateByUrl(route);
  }

  getMessageSegments(content: string): ChatbotMessageSegment[] {
    const segments: ChatbotMessageSegment[] = [];
    const internalRoutePattern = '\\/(?:shop\\/\\d+|orders(?:\\/\\d+)?|my-orders|account\\/orders|order-history|favorites|basket|checkout|chat(?:\\/conversation)?|whats-new|blog(?:\\/\\d+)?)';
    const internalLinkPattern = new RegExp(
      `\\[([^\\]]+)\\](\\((${internalRoutePattern})\\))|(${internalRoutePattern})|\\b(My Orders)\\b`,
      'gi'
    );
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = internalLinkPattern.exec(content)) !== null) {
      if (match.index > lastIndex) {
        segments.push({ text: content.slice(lastIndex, match.index) });
      }

      const label = match[1] || match[4] || match[5];
      const route = this.normalizeInternalRoute(match[3] || match[4] || '/orders');
      segments.push({
        text: label,
        routerLink: route
      });
      lastIndex = internalLinkPattern.lastIndex;
    }

    if (lastIndex < content.length) {
      segments.push({ text: content.slice(lastIndex) });
    }

    return segments.length > 0 ? segments : [{ text: content }];
  }

  private normalizeInternalRoute(route: string): string {
    const normalizedRoute = route.toLowerCase();

    if (
      normalizedRoute === '/my-orders' ||
      normalizedRoute === '/account/orders' ||
      normalizedRoute === '/order-history'
    ) {
      return '/orders';
    }

    return route;
  }
}
