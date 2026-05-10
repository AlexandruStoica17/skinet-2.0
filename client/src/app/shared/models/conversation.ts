export interface Conversation {
    partnerEmail: string;
    partnerName: string;
    lastMessage: string;
    lastMessageSent: Date;
    unreadCount: number;
    // NOU: fiecare conversatie e legata de o comanda specifica
    orderId?: number;
    orderTitle?: string;
}