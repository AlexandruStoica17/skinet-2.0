namespace API.Dtos
{
    public class ConversationDto
    {
        public string PartnerEmail { get; set; }
        public string PartnerName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageSent { get; set; }
        public int UnreadCount { get; set; }

        // NOU: fiecare conversatie e legata de o comanda specifica
        public int? OrderId { get; set; }
        public string OrderTitle { get; set; } // ex: "Comanda #42"
    }
}