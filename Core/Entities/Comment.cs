using Core.Entities.Identity; // Avem nevoie de asta pentru a accesa AppUser

namespace Core.Entities
{
    public class Comment : BaseEntity
    {
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Legătura cu Postarea (La ce postare a fost lăsat comentariul)
        public int PostId { get; set; }
        public Post Post { get; set; }

        // Legătura cu Autorul (Cine a scris comentariul)
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}