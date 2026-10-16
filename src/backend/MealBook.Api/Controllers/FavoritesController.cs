using System.Security.Claims;
using MealBook.Application.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public sealed class FavoritesController(IFavoriteRepository favorites) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Guid>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Guid>>> Get(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? Unauthorized() : Ok(await favorites.GetRecipeIdsAsync(userId, cancellationToken));
    }

    [HttpPut("{recipeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid recipeId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        return await favorites.AddAsync(userId, recipeId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpDelete("{recipeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid recipeId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        await favorites.RemoveAsync(userId, recipeId, cancellationToken);
        return NoContent();
    }
}
