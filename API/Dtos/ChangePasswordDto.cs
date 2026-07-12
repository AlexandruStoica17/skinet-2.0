using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        [RegularExpression("(?=^.{6,30}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9\\s])(?!.*\\s).*$",
            ErrorMessage = "Password must be 6-30 characters and include uppercase, lowercase, number and special character")]
        public string NewPassword { get; set; }
    }
}
