namespace API.Dtos
{
    public class PostToReturnDto
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Content { get; set; }

        // Imagine de copertă
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public string AuthorName { get; set; }

        // NOU: secțiuni cu imagini multiple
        public IReadOnlyList<PostSectionToReturnDto> Sections { get; set; }
    }
}