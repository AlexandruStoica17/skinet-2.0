using Core.Entities;

namespace Core.Specifications
{
    public class PostsWithAuthorSpecification : BaseSpecification<Post>
    {
        // Constructor pentru toate postările
        public PostsWithAuthorSpecification()
        {
            AddInclude(x => x.AppUser);

            // NOU: includem secțiunile articolului
            AddInclude(x => x.Sections);

            AddOrderByDescending(x => x.CreatedAt);
        }

        // Constructor pentru o postare după ID
        public PostsWithAuthorSpecification(int id) : base(x => x.Id == id)
        {
            AddInclude(x => x.AppUser);

            // NOU: includem secțiunile articolului
            AddInclude(x => x.Sections);
        }
    }
}