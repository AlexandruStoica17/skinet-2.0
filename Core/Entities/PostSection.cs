namespace Core.Entities
{
    // NOU: secțiune dintr-un articol de blog.
    // Fiecare secțiune poate avea text + imagine, similar articolelor tip WikiHow.
    public class PostSection : BaseEntity
    {
        public string Heading { get; set; }
        public string Text { get; set; }

        // Imagine opțională pentru această secțiune
        public string ImageUrl { get; set; }

        // Text scurt sub imagine
        public string Caption { get; set; }

        // Ordinea secțiunilor în articol
        public int DisplayOrder { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}