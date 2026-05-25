using API.Helpers;

namespace API.Dtos
{
    public class SellerProfileDto
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string CompanyName { get; set; }
        public bool IsVerified { get; set; }
        public string DocumentUrl { get; set; }
        public string Description { get; set; }
        public string Story { get; set; }
        public string History { get; set; }
        public string Location { get; set; }
        public string MapUrl { get; set; }
        public string SellerType { get; set; }
        public Pagination<ProductToReturnDto> Products { get; set; }
    }
}
