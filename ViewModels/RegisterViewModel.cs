using System.ComponentModel.DataAnnotations;
using Group4_ReadingComicWeb.Utils;

namespace Group4_ReadingComicWeb.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [MinLength(ValidationRules.UsernameMinLength, 
        ErrorMessage = "Username must be at least 6 characters.")]
    [RegularExpression(@"^.{6,}$", 
        ErrorMessage = "Username must be at least 6 characters.")]
    public string Fullname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(ValidationRules.PasswordMinLength, 
        ErrorMessage = "Password must be at least 6 characters.")]
    [RegularExpression(@"^.{6,}$", 
        ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password), 
        ErrorMessage = "Password confirmation does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
