using Core.Entities;

namespace Core.Specifications
{
    public class FavoriteProductsWithProductSpecification : BaseSpecification<FavoriteProduct>
    {
        // Constructor pentru a vedea toate favoritele unui utilizator
        public FavoriteProductsWithProductSpecification(string userId) 
            : base(x => x.AppUserId == userId)
        {
            AddInclude(x => x.Product);
        }
    }
}