using Core.Entities.Identity;

namespace Core.Entities
{
    public class Message : BaseEntity
    {
        public string SenderId { get; set; }
        public string SenderUsername { get; set; }
        public AppUser Sender { get; set; } // Legătura către user

        public string RecipientId { get; set; }
        public string RecipientUsername { get; set; }
        public AppUser Recipient { get; set; } // Legătura către user

        public string Content { get; set; }
        
        public DateTime? DateRead { get; set; } // Când a fost citit (pentru "Seen")
        public DateTime MessageSent { get; set; } = DateTime.UtcNow;

        // Soft delete: Nu vrem să ștergem mesajul de tot dacă doar un user îl șterge. 
        // Mesajul dispare din DB doar dacă AMBII useri dau delete.
        public bool SenderDeleted { get; set; }
        public bool RecipientDeleted { get; set; }
    }
}