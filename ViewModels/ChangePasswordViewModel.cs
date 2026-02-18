using System.ComponentModel.DataAnnotations;
using Group4_ReadingComicWeb.Utils;

namespace Group4_ReadingComicWeb.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(ValidationRules.PasswordMinLength,
        ErrorMessage = "New password must be at least 6 characters.")]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Password confirmation does not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
