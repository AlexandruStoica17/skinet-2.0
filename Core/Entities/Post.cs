using Core.Entities.Identity;

namespace Core.Entities
{
    public class Post : BaseEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Legătura cu utilizatorul (Autorul)
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
}