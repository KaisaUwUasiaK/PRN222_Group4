using System.Text.RegularExpressions;

namespace Group4_ReadingComicWeb.Utils;

public static class ValidationRules
{
    // Vietnamese-friendly: letters (Unicode), digits, spaces. No special characters.
    public const int UsernameMinLength = 6;
    public static readonly Regex UsernameRegex = new(
        pattern: @"^[\p{L}\p{Nd} ]+$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // >= 6 chars, at least 1 letter and 1 digit. Allows other characters too.
    public const int PasswordMinLength = 6;
    public static readonly Regex PasswordRegex = new(
        pattern: @"^(?=.*[\p{L}])(?=.*\d).{6,}$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizeSpaces(string input)
        => Regex.Replace(input.Trim(), @"\s+", " ");
}

