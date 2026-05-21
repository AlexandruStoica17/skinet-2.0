using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ProductEditDto
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

        // NOU: upload multiplu pentru pozele produsului
        public List<IFormFile> Pictures { get; set; } = new();
    }
}