using System.ComponentModel.DataAnnotations;
using Group4_ReadingComicWeb.Utils;

namespace Group4_ReadingComicWeb.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên.")]
    [MinLength(ValidationRules.UsernameMinLength, ErrorMessage = "Tên phải có ít nhất 6 ký tự.")]
    [RegularExpression(@"^[\p{L}\p{Nd} ]+$", ErrorMessage = "Tên chỉ được chứa chữ (có dấu), số và khoảng trắng.")]
    public string Fullname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(ValidationRules.PasswordMinLength, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [RegularExpression(@"^(?=.*[\p{L}])(?=.*\d).{6,}$", ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ và 1 số.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

