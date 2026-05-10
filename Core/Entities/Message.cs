using Core.Entities.Identity;

namespace Core.Entities
{
    public class Message : BaseEntity
    {
        public string SenderId { get; set; }
        public string SenderUsername { get; set; }
        public AppUser Sender { get; set; }

        public string RecipientId { get; set; }
        public string RecipientUsername { get; set; }
        public AppUser Recipient { get; set; }

        public string Content { get; set; }
        
        public DateTime? DateRead { get; set; }
        public DateTime MessageSent { get; set; } = DateTime.UtcNow;

        public bool SenderDeleted { get; set; }
        public bool RecipientDeleted { get; set; }

        // NOU: leaga mesajul de o comanda specifica — fiecare comanda = conversatie separata
        public int? OrderId { get; set; }

        // NOU: marcheaza mesajul ca prompt de review (apare buton "Lasa review")
        public bool IsReviewPrompt { get; set; } = false;
    }
}