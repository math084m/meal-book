namespace MealBook.Application.Accounts;

public sealed record CurrentUserResponse(string Id, string Email, string? DisplayName, bool IsAdmin);

// Validated by the register endpoint so every field error is reported at once.
public sealed class RegisterRequest
{
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string? Password { get; init; }
}

public sealed class UpdateProfileRequest
{
    public string? DisplayName { get; init; }
}
