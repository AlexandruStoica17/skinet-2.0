namespace API.Dtos
{
    // Reprezintă o conversație rezumată în inbox:
    // partenerul + ultimul mesaj + număr necitite
    public class ConversationDto
    {
        public string PartnerEmail { get; set; }
        public string PartnerName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageSent { get; set; }
        public int UnreadCount { get; set; }
    }
}