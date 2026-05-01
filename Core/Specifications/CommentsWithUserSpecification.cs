using Core.Entities;

namespace Core.Specifications
{
    public class CommentsWithUserSpecification : BaseSpecification<Comment>
    {
        public CommentsWithUserSpecification(int postId) : base(x => x.PostId == postId)
        {
            AddInclude(x => x.AppUser);
            // Dacă vrei, adaugă și linia de mai jos pentru ordonare (cele mai noi primele)
             AddOrderByDescending(x => x.CreatedAt); 
        }
    }
}