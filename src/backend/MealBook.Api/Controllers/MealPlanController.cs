using System.Security.Claims;
using MealBook.Application.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

// One shared household plan: any signed-in user can view and change it.
[ApiController]
[Route("api/meal-plan")]
[Authorize]
public sealed class MealPlanController(IMealPlanService plan) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MealPlanResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MealPlanResponse>> Get(
        [FromQuery] string? lang,
        CancellationToken cancellationToken) =>
        Ok(await plan.GetPlanAsync(lang, cancellationToken));

    [HttpGet("suggestions")]
    [ProducesResponseType<IReadOnlyList<PlanSuggestionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanSuggestionResponse>>> GetSuggestions(
        [FromQuery] string? lang,
        CancellationToken cancellationToken) =>
        Ok(await plan.GetSuggestionsAsync(lang, cancellationToken));

    [HttpPut("recipes/{recipeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(
        Guid recipeId,
        PlanAddRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        return await plan.AddAsync(recipeId, request?.Servings, userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPut("recipes/{recipeId:guid}/servings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetServings(
        Guid recipeId,
        PlanServingsRequest request,
        CancellationToken cancellationToken) =>
        await plan.SetServingsAsync(recipeId, request.Servings, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("recipes/{recipeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid recipeId, CancellationToken cancellationToken) =>
        await plan.RemoveAsync(recipeId, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await plan.ClearAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("checks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetChecked(ShoppingCheckRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        await plan.SetCheckedAsync(request.IngredientId, request.Unit, request.Checked, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("have")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetHave(ShoppingHaveRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        await plan.SetHaveAsync(request.IngredientId, request.Unit, request.Quantity, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("checks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearChecks(CancellationToken cancellationToken)
    {
        await plan.ClearChecksAsync(cancellationToken);
        return NoContent();
    }
}
