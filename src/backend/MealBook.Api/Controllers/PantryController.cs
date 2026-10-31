using System.Security.Claims;
using MealBook.Application.Pantry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

[ApiController]
[Route("api/pantry")]
[Authorize]
public sealed class PantryController(IPantryRepository pantry) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PantryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PantryResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? Unauthorized() : Ok(await pantry.GetAsync(userId, cancellationToken));
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Settings(PantrySettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        await pantry.SetUseForShoppingAsync(userId, request.UseForShopping, cancellationToken);
        return NoContent();
    }

    [HttpPut("ingredients/{ingredientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid ingredientId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        return await pantry.AddAsync(userId, ingredientId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpDelete("ingredients/{ingredientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid ingredientId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        await pantry.RemoveAsync(userId, ingredientId, cancellationToken);
        return NoContent();
    }

    [HttpPut("ingredients/{ingredientId:guid}/shopping")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Shopping(
        Guid ingredientId, PantryHaveRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        return await pantry.SetHaveFractionAsync(userId, ingredientId, request.HaveFraction, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
