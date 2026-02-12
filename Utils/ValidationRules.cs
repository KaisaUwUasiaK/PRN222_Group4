using System.Text.RegularExpressions;

namespace Group4_ReadingComicWeb.Utils;

public static class ValidationRules
{
    // Username: minimum 6 characters
    public const int UsernameMinLength = 6;
    public static readonly Regex UsernameRegex = new(
        pattern: @"^.{6,}$",
        options: RegexOptions.Compiled);

    // Password: minimum 6 characters
    public const int PasswordMinLength = 6;
    public static readonly Regex PasswordRegex = new(
        pattern: @"^.{6,}$",
        options: RegexOptions.Compiled);

    public static string NormalizeSpaces(string input)
        => Regex.Replace(input.Trim(), @"\s+", " ");
}
