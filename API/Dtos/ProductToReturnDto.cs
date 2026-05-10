namespace API.Dtos
{
    public class ProductToReturnDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string PictureUrl { get; set; }
       public string ProductType { get; set; }
        public int ProductTypeId { get; set; } // <--- ADAUGĂ ASTA

        public string ProductBrand { get; set; }
        public int ProductBrandId { get; set; } // <--- ADAUGĂ ASTA
        public string ProducerName { get; set; }
         public string ProducerEmail { get; set; } // NOU: necesar pentru butonul de chat
    }
}