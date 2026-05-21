using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class PostCreateDto
    {
        [Required]
        public string Title { get; set; }

        // Păstrat pentru compatibilitate/search.
        // Îl completăm automat din secțiuni dacă vine gol.
        public string Content { get; set; }

        // Imagine copertă, opțională
        public IFormFile Image { get; set; }

        // JSON cu secțiunile articolului.
        // Imaginile secțiunilor se trimit separat ca fișiere:
        // sectionImages_0, sectionImages_1, sectionImages_2...
        public string SectionsJson { get; set; }
    }
}