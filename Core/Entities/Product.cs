namespace Core.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        // Păstrăm PictureUrl pentru compatibilitate cu ce ai deja în shop/card/basket.
        public string PictureUrl { get; set; }

        public ProductType ProductType { get; set; }
        public int ProductTypeId { get; set; }

        public ProductBrand ProductBrand { get; set; }
        public int ProductBrandId { get; set; }

        // --- Aici salvăm ID-ul producătorului care a adăugat produsul ---
        public string ProducerId { get; set; }
        public string ProducerName { get; set; }

        // NOU: data creării produsului, folosită pentru What's New
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Filtre produs
        public string SkinType { get; set; } // e.g. "Oily, Dry, Combination, All"
        public string Usage { get; set; } // e.g. "Face, Hands, Body, Eyes"
        public string Benefits { get; set; } // e.g. "Hydration, SPF, Brightening"
        public string Formula { get; set; }

        // NOU: mai multe poze pentru produs
        public ICollection<ProductPhoto> Photos { get; set; } = new List<ProductPhoto>();
    }
}