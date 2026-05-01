export interface Message {
    id: number;
    senderId: string;
    senderUsername: string;
    recipientId: string;
    recipientUsername: string;
    content: string;
    dateRead?: Date;
    messageSent: Date;
}