using Core.Entities;

namespace Core.Specifications
{
    public class ProductsWithTypesAndBrandsSpecification : BaseSpecification<Product>
    {
        public ProductsWithTypesAndBrandsSpecification(ProductSpecParams productParams, bool applyPaging = true)
            : base(x =>
                // Search
                (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search)) &&

                // Brand
                (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&

                // Type
                (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId) &&

                // Price range
                (productParams.MinPrice == 0 || x.Price >= productParams.MinPrice) &&
                (productParams.MaxPrice == 0 || x.Price <= productParams.MaxPrice)
            )
        {
            AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);

            AddOrderBy(x => x.Name);

            // MODIFICAT: nu aplicăm paging aici când avem nevoie de filtrare in-memory.
            if (applyPaging)
            {
                ApplyPaging(productParams.PageSize * (productParams.PageIndex - 1), productParams.PageSize);
            }

            if (!string.IsNullOrEmpty(productParams.Sort))
            {
                switch (productParams.Sort)
                {
                    case "priceAsc":
                        AddOrderBy(p => p.Price);
                        break;

                    case "priceDesc":
                        AddOrderByDescending(p => p.Price);
                        break;

                    default:
                        AddOrderBy(n => n.Name);
                        break;
                }
            }
        }

        public ProductsWithTypesAndBrandsSpecification(int id)
            : base(x => x.Id == id)
        {
            AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);
        }
    }
}