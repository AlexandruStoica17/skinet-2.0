namespace API.Dtos
{
    public class PostSectionToReturnDto
    {
        public int Id { get; set; }

        public string Heading { get; set; }
        public string Text { get; set; }

        public string ImageUrl { get; set; }
        public string Caption { get; set; }

        public int DisplayOrder { get; set; }
    }
}