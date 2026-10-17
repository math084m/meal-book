using MealBook.Application.Comments;
using MealBook.Domain.Comments;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class CommentRepository(RecipeDbContext db) : ICommentRepository
{
    public async Task<IReadOnlyList<RecipeCommentResponse>?> GetForRecipeAsync(
        Guid recipeId,
        CommentViewer viewer,
        CancellationToken cancellationToken)
    {
        if (!await IsPublishedAsync(recipeId, cancellationToken))
        {
            return null;
        }

        var rows = await (
            from comment in db.Set<RecipeComment>().AsNoTracking()
            join user in db.Users.AsNoTracking() on comment.UserId equals user.Id
            where comment.RecipeId == recipeId
            orderby comment.CreatedAt
            select new { Comment = comment, user.DisplayName })
            .ToListAsync(cancellationToken);

        var replies = rows
            .Where(row => row.Comment.ParentId is not null)
            .ToLookup(
                row => row.Comment.ParentId!.Value,
                row => ToResponse(row.Comment, row.DisplayName, viewer, []));
        return rows
            .Where(row => row.Comment.ParentId is null)
            .Select(row => ToResponse(row.Comment, row.DisplayName, viewer, replies[row.Comment.Id].ToArray()))
            .ToArray();
    }

    public async Task<CommentWriteResult> AddAsync(
        Guid recipeId,
        CommentViewer viewer,
        string body,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (!await IsPublishedAsync(recipeId, cancellationToken))
        {
            return new CommentWriteResult(CommentWriteStatus.RecipeNotFound);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Guid? threadId = null;
        if (parentId is { } requestedParent)
        {
            await LockThreadAsync(requestedParent, cancellationToken);
            var parent = await db.Set<RecipeComment>()
                .AsNoTracking()
                .Where(item => item.Id == requestedParent && item.RecipeId == recipeId && item.DeletedAt == null)
                .Select(item => new { item.Id, item.ParentId })
                .SingleOrDefaultAsync(cancellationToken);
            if (parent is null)
            {
                return new CommentWriteResult(CommentWriteStatus.CommentNotFound);
            }

            threadId = parent.ParentId ?? parent.Id;
        }

        var displayName = await GetDisplayNameAsync(viewer.UserId, cancellationToken);
        if (displayName is null)
        {
            return new CommentWriteResult(CommentWriteStatus.DisplayNameRequired);
        }

        var comment = RecipeComment.Create(recipeId, viewer.UserId, body, threadId);
        db.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CommentWriteResult(CommentWriteStatus.Saved, ToResponse(comment, displayName, viewer, []));
    }

    public async Task<CommentWriteResult> UpdateAsync(
        Guid commentId,
        CommentViewer viewer,
        string body,
        CancellationToken cancellationToken)
    {
        var comment = await db.Set<RecipeComment>()
            .SingleOrDefaultAsync(item => item.Id == commentId && item.DeletedAt == null, cancellationToken);
        if (comment is null)
        {
            return new CommentWriteResult(CommentWriteStatus.CommentNotFound);
        }

        if (comment.UserId != viewer.UserId)
        {
            return new CommentWriteResult(CommentWriteStatus.Forbidden);
        }

        comment.Edit(body);
        await db.SaveChangesAsync(cancellationToken);
        var displayName = await GetDisplayNameAsync(viewer.UserId, cancellationToken);
        return new CommentWriteResult(CommentWriteStatus.Saved, ToResponse(comment, displayName, viewer, []));
    }

    public async Task<CommentWriteResult> DeleteAsync(
        Guid commentId,
        CommentViewer viewer,
        CancellationToken cancellationToken)
    {
        var comments = db.Set<RecipeComment>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockThreadAsync(commentId, cancellationToken);
        var comment = await comments
            .SingleOrDefaultAsync(item => item.Id == commentId && item.DeletedAt == null, cancellationToken);
        if (comment is null)
        {
            return new CommentWriteResult(CommentWriteStatus.CommentNotFound);
        }

        if (comment.UserId != viewer.UserId && !viewer.IsAdmin)
        {
            return new CommentWriteResult(CommentWriteStatus.Forbidden);
        }

        if (comment.ParentId is { } parentId)
        {
            comments.Remove(comment);

            // A placeholder left behind for this thread is no longer needed once its last reply is gone.
            var parent = await comments.SingleAsync(item => item.Id == parentId, cancellationToken);
            if (parent.IsDeleted
                && !await comments.AnyAsync(item => item.ParentId == parentId && item.Id != comment.Id, cancellationToken))
            {
                comments.Remove(parent);
            }
        }
        else if (await comments.AnyAsync(item => item.ParentId == comment.Id, cancellationToken))
        {
            comment.MarkDeleted();
        }
        else
        {
            comments.Remove(comment);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CommentWriteResult(CommentWriteStatus.Saved);
    }

    // Serialises replies and deletions within one thread by locking its root row, so concurrent
    // writers cannot strand an empty placeholder or attach a reply to a root being removed.
    private Task LockThreadAsync(Guid commentId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync(
            $"""
            SELECT 1 FROM "RecipeComments"
            WHERE "Id" = (SELECT COALESCE("ParentId", "Id") FROM "RecipeComments" WHERE "Id" = {commentId})
            FOR UPDATE
            """,
            cancellationToken);

    private Task<bool> IsPublishedAsync(Guid recipeId, CancellationToken cancellationToken) =>
        db.Recipes.AnyAsync(recipe => recipe.Id == recipeId && recipe.IsPublished, cancellationToken);

    private Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken) =>
        db.Users.Where(user => user.Id == userId).Select(user => user.DisplayName).SingleOrDefaultAsync(cancellationToken);

    private static RecipeCommentResponse ToResponse(
        RecipeComment comment,
        string? authorName,
        CommentViewer viewer,
        IReadOnlyList<RecipeCommentResponse> replies)
    {
        if (comment.IsDeleted)
        {
            return new RecipeCommentResponse(
                comment.Id, comment.ParentId, null, string.Empty, comment.CreatedAt, null, true, false, false, replies);
        }

        var isAuthor = comment.UserId == viewer.UserId;
        return new RecipeCommentResponse(
            comment.Id,
            comment.ParentId,
            authorName,
            comment.Body,
            comment.CreatedAt,
            comment.UpdatedAt,
            false,
            isAuthor,
            isAuthor || viewer.IsAdmin,
            replies);
    }
}
