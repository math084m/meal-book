using System.Text.RegularExpressions;

namespace MealBook.Application.Accounts;

// Public names shown next to comments and in the account menu.
public static partial class DisplayNames
{
    public const int MinLength = 2;
    public const int MaxLength = 30;

    // Collapses inner whitespace and trims. Returns false with a user-facing error when the name is unusable.
    public static bool TryNormalize(string? value, out string normalized, out string? error)
    {
        normalized = Whitespace().Replace(value ?? string.Empty, " ").Trim();
        error = normalized.Length switch
        {
            < MinLength => $"Display name must contain at least {MinLength} characters.",
            > MaxLength => $"Display name must contain at most {MaxLength} characters.",
            _ when !Allowed().IsMatch(normalized) =>
                "Display name may only contain letters, numbers, spaces, and . _ - ' and must start and end with a letter or number.",
            _ => null
        };
        return error is null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^[\p{L}\p{N}](?:[\p{L}\p{N} ._'-]*[\p{L}\p{N}])?$")]
    private static partial Regex Allowed();
}
