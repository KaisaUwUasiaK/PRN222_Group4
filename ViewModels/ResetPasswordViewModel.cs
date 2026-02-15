using System.ComponentModel.DataAnnotations;
using Group4_ReadingComicWeb.Utils;

namespace Group4_ReadingComicWeb.ViewModels;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(ValidationRules.PasswordMinLength,
        ErrorMessage = "New password must be at least 6 characters.")]

    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password),
        ErrorMessage = "Password confirmation does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
