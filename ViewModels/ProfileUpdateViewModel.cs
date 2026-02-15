using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Utils;

namespace Group4_ReadingComicWeb.ViewModels;

public class ProfileUpdateViewModel : IValidatableObject
{
    [ValidateNever]
    public User User { get; set; } = null!;

    [Required(ErrorMessage = "Username is required.")]
    [MinLength(ValidationRules.UsernameMinLength, 
        ErrorMessage = "Username must be at least 6 characters.")]
    [RegularExpression(@"^.{6,}$", 
        ErrorMessage = "Username must be at least 6 characters.")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Bio must be less than 500 characters.")]
    public string? Bio { get; set; }

    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Only validate password if user enters it
        if (!string.IsNullOrWhiteSpace(NewPassword) || 
            !string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                yield return new ValidationResult(
                    "New password is required.", 
                    new[] { nameof(NewPassword) });
                yield break;
            }

            if (NewPassword.Length < ValidationRules.PasswordMinLength)
            {
                yield return new ValidationResult(
                    "Password must be at least 6 characters.", 
                    new[] { nameof(NewPassword) });
            }

            if (!string.Equals(NewPassword, ConfirmPassword, 
                StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "Password confirmation does not match.", 
                    new[] { nameof(ConfirmPassword) });
            }
        }

        // Extra guard after normalization
        if (!string.IsNullOrWhiteSpace(Username))
        {
            var normalized = ValidationRules.NormalizeSpaces(Username);
            if (normalized.Length < ValidationRules.UsernameMinLength)
            {
                yield return new ValidationResult(
                    "Username must be at least 6 characters.",
                    new[] { nameof(Username) });
            }
        }
    }
}
