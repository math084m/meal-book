using System.Security.Claims;
using MealBook.Application.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

[ApiController]
[Authorize]
public sealed class CommentsController(ICommentRepository comments) : ControllerBase
{
    [HttpGet("api/recipes/{recipeId:guid}/comments")]
    [ProducesResponseType<IReadOnlyList<RecipeCommentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RecipeCommentResponse>>> Get(
        Guid recipeId,
        CancellationToken cancellationToken)
    {
        if (Viewer() is not { } viewer)
        {
            return Unauthorized();
        }

        var list = await comments.GetForRecipeAsync(recipeId, viewer, cancellationToken);
        return list is null ? NotFound() : Ok(list);
    }

    [HttpPost("api/recipes/{recipeId:guid}/comments")]
    [ProducesResponseType<RecipeCommentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        Guid recipeId,
        CreateRecipeCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (Viewer() is not { } viewer)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return EmptyBody();
        }

        return ToResult(
            await comments.AddAsync(recipeId, viewer, request.Body, request.ParentId, cancellationToken),
            created: true);
    }

    [HttpPut("api/comments/{id:guid}")]
    [ProducesResponseType<RecipeCommentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateRecipeCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (Viewer() is not { } viewer)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return EmptyBody();
        }

        return ToResult(await comments.UpdateAsync(id, viewer, request.Body, cancellationToken));
    }

    [HttpDelete("api/comments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (Viewer() is not { } viewer)
        {
            return Unauthorized();
        }

        var result = await comments.DeleteAsync(id, viewer, cancellationToken);
        return result.Status == CommentWriteStatus.Saved ? NoContent() : ToResult(result);
    }

    private CommentViewer? Viewer() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId
            ? new CommentViewer(userId, User.IsInRole("Admin"))
            : null;

    private IActionResult EmptyBody() =>
        ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["body"] = ["Write something before posting."]
        }));

    private IActionResult ToResult(CommentWriteResult result, bool created = false) => result.Status switch
    {
        CommentWriteStatus.Saved when created => StatusCode(StatusCodes.Status201Created, result.Comment),
        CommentWriteStatus.Saved => Ok(result.Comment),
        CommentWriteStatus.Forbidden => Forbid(),
        CommentWriteStatus.DisplayNameRequired => Conflict(new ProblemDetails
        {
            Title = "Choose a display name before commenting.",
            Status = StatusCodes.Status409Conflict
        }),
        _ => NotFound()
    };
}
