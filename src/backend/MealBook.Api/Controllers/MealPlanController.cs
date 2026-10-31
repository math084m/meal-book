using System.Security.Claims;
using MealBook.Application.Pantry;
using MealBook.Application.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

// One shared household plan: any signed-in user can view and change it.
[ApiController]
[Route("api/meal-plan")]
[Authorize]
public sealed class MealPlanController(IMealPlanService plan, IPantryRepository pantry) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MealPlanResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MealPlanResponse>> Get(
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var sharedPlan = await plan.GetPlanAsync(lang, cancellationToken);
        return Ok(PantryShopping.Apply(sharedPlan, await pantry.GetAsync(userId, cancellationToken)));
    }

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

    [HttpPut("recipes/{recipeId:guid}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Schedule(
        Guid recipeId,
        PlanScheduleRequest request,
        CancellationToken cancellationToken) =>
        await plan.ScheduleAsync(recipeId, request.Start, request.Days, cancellationToken)
            ? NoContent()
            : NotFound();

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

    [HttpPost("custom")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddCustom(ShoppingCustomItemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        return await plan.AddCustomItemAsync(request, userId, cancellationToken)
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = "Unknown shop section or empty name." });
    }

    [HttpPut("custom/{id:guid}/checked")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCustomChecked(
        Guid id,
        ShoppingCustomCheckRequest request,
        CancellationToken cancellationToken) =>
        await plan.SetCustomItemCheckedAsync(id, request.Checked, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("custom/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCustom(Guid id, CancellationToken cancellationToken) =>
        await plan.RemoveCustomItemAsync(id, cancellationToken) ? NoContent() : NotFound();

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
