export interface Message {
    id: number;
    senderId: string;
    senderUsername: string;
    recipientId: string;
    recipientUsername: string;
    content: string;
    dateRead?: Date;
    messageSent: Date;
    // NOU: pentru butonul de review
    isReviewPrompt?: boolean;
    orderId?: number;
    isSystemMessage?: boolean;
}