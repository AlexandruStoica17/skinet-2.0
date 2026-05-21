namespace API.Dtos
{
    public class ProductPhotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public bool IsMain { get; set; }

        // NOU
        public int DisplayOrder { get; set; }
    }
}