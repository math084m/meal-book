using System.ComponentModel.DataAnnotations;
using MealBook.Domain.Comments;

namespace MealBook.Application.Comments;

public sealed record CommentViewer(string UserId, bool IsAdmin);

// A top-level comment carries its replies (oldest first); replies have an empty Replies list.
// Deleted comments that still have replies come back with IsDeleted = true, no author and no body.
public sealed record RecipeCommentResponse(
    Guid Id,
    Guid? ParentId,
    string? AuthorName,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsDeleted,
    bool CanEdit,
    bool CanDelete,
    IReadOnlyList<RecipeCommentResponse> Replies);

public sealed class CreateRecipeCommentRequest
{
    [Required]
    [StringLength(RecipeComment.MaxBodyLength)]
    public string Body { get; init; } = string.Empty;

    // Reply to this comment. Replying to a reply joins the same thread.
    public Guid? ParentId { get; init; }
}

public sealed class UpdateRecipeCommentRequest
{
    [Required]
    [StringLength(RecipeComment.MaxBodyLength)]
    public string Body { get; init; } = string.Empty;
}

public enum CommentWriteStatus
{
    Saved,
    RecipeNotFound,
    CommentNotFound,
    Forbidden,
    DisplayNameRequired
}

public sealed record CommentWriteResult(CommentWriteStatus Status, RecipeCommentResponse? Comment = null);

public interface ICommentRepository
{
    // Null when the recipe does not exist or is an unpublished draft. Threads oldest first.
    Task<IReadOnlyList<RecipeCommentResponse>?> GetForRecipeAsync(
        Guid recipeId,
        CommentViewer viewer,
        CancellationToken cancellationToken);

    // CommentNotFound when the parent is missing, deleted, or belongs to another recipe.
    Task<CommentWriteResult> AddAsync(
        Guid recipeId,
        CommentViewer viewer,
        string body,
        Guid? parentId,
        CancellationToken cancellationToken);

    // Only the author may edit.
    Task<CommentWriteResult> UpdateAsync(
        Guid commentId,
        CommentViewer viewer,
        string body,
        CancellationToken cancellationToken);

    // The author or an admin may delete.
    Task<CommentWriteResult> DeleteAsync(
        Guid commentId,
        CommentViewer viewer,
        CancellationToken cancellationToken);
}
