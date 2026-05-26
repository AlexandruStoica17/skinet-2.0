export type ChatbotRole = 'user' | 'assistant';

export interface ChatbotHistoryMessage {
  role: ChatbotRole;
  content: string;
}

export interface ChatbotMessage extends ChatbotHistoryMessage {
  sources?: string[];
  mode?: string;
  isWelcome?: boolean;
}

export interface ChatbotResponse {
  answer: string;
  sources: string[];
  mode: string;
  isAiConfigured: boolean;
}
