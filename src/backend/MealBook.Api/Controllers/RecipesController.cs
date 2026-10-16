using System.Security.Claims;
using MealBook.Application.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RecipeValidationFilter]
public sealed class RecipesController(IRecipeService recipes) : ControllerBase
{
    [HttpGet("suggestions")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<RecipeSuggestionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecipeSuggestionResponse>>> GetSuggestions(
        [FromQuery] string? q,
        [FromQuery] string? lang,
        CancellationToken cancellationToken) =>
        Ok(await recipes.GetSuggestionsAsync(q, lang, cancellationToken));

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<RecipeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecipeResponse>>> GetAll(
        [FromQuery] RecipeSearchRequest search,
        CancellationToken cancellationToken) =>
        Ok(await recipes.GetAllAsync(search, cancellationToken));

    [HttpGet("admin/drafts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IReadOnlyList<RecipeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecipeResponse>>> GetDrafts(
        [FromQuery] string? lang,
        CancellationToken cancellationToken) =>
        Ok(await recipes.GetDraftsAsync(lang, cancellationToken));

    [HttpGet("dashboard")]
    [AllowAnonymous]
    [ProducesResponseType<RecipeDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RecipeDashboardResponse>> GetDashboard(
        [FromQuery] string? lang,
        CancellationToken cancellationToken) =>
        Ok(await recipes.GetDashboardAsync(lang, cancellationToken));

    [HttpGet("cooking-history")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<RecipeCookedResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecipeCookedResponse>>> GetCookHistory(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await recipes.GetCookHistoryAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeResponse>> Get(
        Guid id,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetByIdAsync(id, lang, cancellationToken);
        if (recipe is { IsPublished: false } && !User.IsInRole("Admin"))
        {
            return NotFound();
        }

        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecipeResponse>> Create(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipes.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = recipe.Id }, recipe);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeResponse>> Update(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = await recipes.UpdateAsync(id, request, cancellationToken);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeResponse>> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var recipe = await recipes.PublishAsync(id, cancellationToken);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await recipes.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/cooked")]
    [Authorize]
    [ProducesResponseType<RecipeCookedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeCookedResponse>> LogCooked(
        Guid id,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var entry = await recipes.LogCookedAsync(id, userId, request, cancellationToken);
        return entry is null
            ? NotFound()
            : StatusCode(StatusCodes.Status201Created, entry);
    }
}
