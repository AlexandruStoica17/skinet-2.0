using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class RegisterDto
    {
        [Required]
        public string DisplayName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [RegularExpression("(?=^.{6,30}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9\\s])(?!.*\\s).*$",
        ErrorMessage = "Password must be 6-30 characters and include uppercase, lowercase, number and special character")]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; } // Angular ne va trimite "Buyer", "CosmeticsProducer", etc.

        public string CompanyName { get; set; } 

    }
}
