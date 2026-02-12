using System.ComponentModel.DataAnnotations;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Utils;

namespace Group4_ReadingComicWeb.ViewModels;

public class ProfileUpdateViewModel : IValidatableObject
{
    // Current user data for display
    public User User { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập tên.")]
    [MinLength(ValidationRules.UsernameMinLength, ErrorMessage = "Tên phải có ít nhất 6 ký tự.")]
    [RegularExpression(@"^[\p{L}\p{Nd} ]+$", ErrorMessage = "Tên chỉ được chứa chữ (có dấu), số và khoảng trắng.")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Bio tối đa 500 ký tự.")]
    public string? Bio { get; set; }

    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Conditional password validation: only when user enters a new password.
        if (!string.IsNullOrWhiteSpace(NewPassword) || !string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                yield return new ValidationResult("Vui lòng nhập mật khẩu mới.", new[] { nameof(NewPassword) });
                yield break;
            }

            var pwd = NewPassword;
            if (pwd.Length < ValidationRules.PasswordMinLength || !ValidationRules.PasswordRegex.IsMatch(pwd))
            {
                yield return new ValidationResult("Mật khẩu phải có ít nhất 6 ký tự và gồm ít nhất 1 chữ + 1 số.", new[] { nameof(NewPassword) });
            }

            if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult("Mật khẩu xác nhận không khớp.", new[] { nameof(ConfirmPassword) });
            }
        }

        // Normalize: guard against username that is only spaces
        if (!string.IsNullOrWhiteSpace(Username))
        {
            var normalized = ValidationRules.NormalizeSpaces(Username);
            if (normalized.Length < ValidationRules.UsernameMinLength)
            {
                yield return new ValidationResult("Tên phải có ít nhất 6 ký tự.", new[] { nameof(Username) });
            }
        }
    }
}

