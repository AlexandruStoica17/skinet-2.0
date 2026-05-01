namespace API.Dtos
{
    public class CommentDto
    {
        public string Content { get; set; }
        public int PostId { get; set; }
        public string AppUserId { get; set; } // Temporar, până activăm Token-ul
    }
}