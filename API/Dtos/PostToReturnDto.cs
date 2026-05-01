namespace API.Dtos
{
    public class PostToReturnDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Aici trimitem doar numele autorului, nu tot obiectul AppUser!
        public string AuthorName { get; set; } 
    }
}