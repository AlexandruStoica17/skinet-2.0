using Core.Entities;

namespace Core.Specifications
{
    public class PostsWithAuthorSpecification : BaseSpecification<Post>
    {
        // Constructorul pentru a lua TOATE postările
        public PostsWithAuthorSpecification()
        {
            AddInclude(x => x.AppUser);
            AddOrderByDescending(x => x.CreatedAt); // Cele mai noi primele
        }

        // Constructorul pentru a lua o SINGURĂ postare după ID
        public PostsWithAuthorSpecification(int id) : base(x => x.Id == id)
        {
            AddInclude(x => x.AppUser);
        }
    }
}