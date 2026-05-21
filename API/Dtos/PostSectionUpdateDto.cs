namespace API.Dtos
{
    public class PostSectionUpdateDto
    {
        public int? Id { get; set; }

        public string Heading { get; set; }
        public string Text { get; set; }
        public string Caption { get; set; }

        public int DisplayOrder { get; set; }

        // NOU: dacă vrei să ștergi imaginea unei secțiuni fără să ștergi secțiunea
        public bool RemoveImage { get; set; }
    }
}