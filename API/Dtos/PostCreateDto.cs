using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class PostCreateDto
    {
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        // Temporar vom trimite ID-ul autorului de mână.
        // Mai târziu îl vom extrage automat din Token-ul de login!
        [Required]
        public string AppUserId { get; set; } 
    }
}