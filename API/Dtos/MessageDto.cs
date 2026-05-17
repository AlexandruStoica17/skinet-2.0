namespace API.Dtos
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderUsername { get; set; }
        public string RecipientId { get; set; }
        public string RecipientUsername { get; set; }
        public string Content { get; set; }
        public DateTime? DateRead { get; set; }
        public DateTime MessageSent { get; set; }

        // NOU
        public int? OrderId { get; set; }
        public bool IsReviewPrompt { get; set; }
        public bool IsSystemMessage { get; set; }
    }
}