using Core.Entities.Identity;

namespace Core.Entities
{
    public class Post : BaseEntity
    {
        public string Title { get; set; }

        // Păstrăm Content pentru compatibilitate cu articolele vechi și pentru search/sugestii.
        public string Content { get; set; }

        // Imagine de copertă pentru cardul de blog
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // NOU: secțiuni cu text + imagini multiple
        public ICollection<PostSection> Sections { get; set; } = new List<PostSection>();
    }
}