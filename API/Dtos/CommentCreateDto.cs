namespace API.Dtos
{
    // DTO pentru a primi comentariul de la utilizator
    public class CommentCreateDto
    {
        public string Content { get; set; }
        public int PostId { get; set; }
        public string AppUserId { get; set; } // Îl trimitem manual acum, din Token mai târziu
    }

    // DTO pentru a afișa comentariul (Return)
    public class CommentToReturnDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }
    }
}