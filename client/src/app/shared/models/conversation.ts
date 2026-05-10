export interface Conversation {
    partnerEmail: string;
    partnerName: string;
    lastMessage: string;
    lastMessageSent: Date;
    unreadCount: number;
}