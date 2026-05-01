using Core.Entities.Identity;

namespace Core.Entities
{
    public class Photo : BaseEntity
    {
        public string Url { get; set; }
        public bool IsMain { get; set; }
        public string PublicId { get; set; } // Pentru Cloudinary
        
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}