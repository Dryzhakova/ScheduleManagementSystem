using System.ComponentModel.DataAnnotations;

namespace WebAppsMoodle.Models
{
    public class Teacher
    {
        public string TeacherId { get; set; } = Guid.NewGuid().ToString();
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[A-Z][a-z]{2,}$", ErrorMessage = "Username must start with a capital letter and have at least 3 letters.")]
        public string Username { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
        ErrorMessage = "Password must be at least 8 characters long, contain one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }
        public string Title { get; set; }

        public ICollection<Classes> Classes { get; set; } // Связь с таблицей Classes
        public ICollection<UserToken> UserTokens { get; set; }
    }
}
