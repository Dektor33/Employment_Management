
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.DTOs
{
    public class UserProfile
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required, EmailAddress, DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
        public string Image { get; set; } = "images/Profile/defaultProfile_Image.jpg";
    }
}
