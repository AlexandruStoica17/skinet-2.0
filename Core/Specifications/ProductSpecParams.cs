namespace Core.Specifications
{
    public class ProductSpecParams
    {
        private const int MaxPageSize = 50;
        public int PageIndex { get; set; } = 1;

        private int _pageSize = 6;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public int? BrandId { get; set; }
        public int? TypeId { get; set; }
        public string Sort { get; set; }

        private string _search;
        public string Search
        {
            get => _search;
            set => _search = value?.ToLower();
        }

        // Price range
        public decimal MinPrice { get; set; } = 0;
        public decimal MaxPrice { get; set; } = 0;

        // UPDATED: multiselect — comma-separated strings from query
        // e.g. skinTypes=Oily,Dry
        public string SkinTypes { get; set; }   // "Oily,Dry"
        public string Usages { get; set; }       // "Face,Hands"
        public string Benefits { get; set; }     // "Hydration,SPF Protection"
        public string Formulas { get; set; }     // "Cream,Serum"

        // Rating filter
        public int MinRating { get; set; } = 0;
    }
}