using System.Text.RegularExpressions;

namespace Application.Services;

internal static partial class ProfileInputNormalizer
{
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? NormalizePhoneNumber(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized is null
            ? null
            : string.Concat(normalized.Where(character =>
                !char.IsWhiteSpace(character) && character is not '-' and not '(' and not ')'));
    }

    public static bool IsValidPhoneNumber(string? value)
    {
        var normalized = NormalizePhoneNumber(value);
        return normalized is null || PhoneNumberPattern().IsMatch(normalized);
    }

    [GeneratedRegex("^\\+?\\d{8,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneNumberPattern();
}
