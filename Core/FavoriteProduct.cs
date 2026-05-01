using Core.Entities.Identity;

namespace Core.Entities
{
    public class FavoriteProduct : BaseEntity
    {
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}