namespace Core.Entities
{
    public class ProductPhoto : BaseEntity
    {
        public string Url { get; set; }
        public bool IsMain { get; set; }

        // NOU: ordinea pozelor în galerie
        public int DisplayOrder { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}