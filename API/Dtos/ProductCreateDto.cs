using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ProductCreateDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public int ProductTypeId { get; set; }
        
        [Required]
        public int ProductBrandId { get; set; }
        
        // Aici vom primi poza din Angular
        [Required]
        public IFormFile Picture { get; set; } 

         // NEW filters
        public string SkinType { get; set; }
        public string Usage { get; set; }
        public string Benefits { get; set; }
        public string Formula { get; set; }
    }
}