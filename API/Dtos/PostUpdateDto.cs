using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class PostUpdateDto
    {
        [Required]
        public string Title { get; set; }

        // Păstrat pentru compatibilitate/search
        public string Content { get; set; }

        // Imagine copertă nouă, opțională
        public IFormFile Image { get; set; }

        // Dacă vrei să ștergi cover image-ul
        public bool RemoveCoverImage { get; set; }

        // JSON cu secțiunile actualizate
        public string SectionsJson { get; set; }
    }
}