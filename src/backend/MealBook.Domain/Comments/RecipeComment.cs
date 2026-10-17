namespace MealBook.Domain.Comments;

// A comment on a recipe, or a reply to one. Threads are one level deep: replies always point at a top-level comment.
// Readable by every signed-in user; only the author may edit it.
public sealed class RecipeComment
{
    public const int MaxBodyLength = 2000;

    private RecipeComment()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Set when a comment with replies is deleted: the thread stays, the text goes.
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public static RecipeComment Create(Guid recipeId, string userId, string body, Guid? parentId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            ParentId = parentId,
            UserId = userId,
            Body = body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void Edit(string body)
    {
        Body = body.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDeleted()
    {
        Body = string.Empty;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
