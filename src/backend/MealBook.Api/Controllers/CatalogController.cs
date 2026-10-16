using MealBook.Application.Recipes;
using MealBook.Domain.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogController(ICatalogRepository catalog) : ControllerBase
{
    [HttpGet("terms")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<TermResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TermResponse>>> GetTerms(CancellationToken cancellationToken) =>
        Ok((await catalog.GetTermsAsync(cancellationToken))
            .Select(term => new TermResponse(term.Kind, term.Slug, term.NameEn, term.NameDa))
            .ToArray());

    [HttpGet("ingredients")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<IngredientResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IngredientResponse>>> GetIngredients(
        CancellationToken cancellationToken) =>
        Ok((await catalog.GetIngredientsAsync(cancellationToken)).Select(ToResponse).ToArray());

    [HttpGet("shop-sections")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<ShopSectionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShopSectionResponse>>> GetShopSections(
        CancellationToken cancellationToken) =>
        Ok((await catalog.GetShopSectionsAsync(cancellationToken))
            .Select(section => new ShopSectionResponse(
                section.Slug,
                section.NameEn,
                section.NameDa,
                section.Position,
                section.Perishable))
            .ToArray());

    [HttpPost("ingredients")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IngredientResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IngredientResponse>> CreateIngredient(
        IngredientWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (await catalog.IngredientNameExistsAsync(request.NameEn, null, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Ingredient already exists." });
        }

        var section = await ResolveSectionAsync(request.ShopSectionSlug, cancellationToken);
        if (section is null)
        {
            return BadRequest(new ProblemDetails { Title = "Unknown shop section." });
        }

        var ingredient = await catalog.AddIngredientAsync(
            Ingredient.Create(request.NameEn, request.NameDa, section),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ToResponse(ingredient));
    }

    [HttpPut("ingredients/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IngredientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IngredientResponse>> UpdateIngredient(
        Guid id,
        IngredientWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (await catalog.IngredientNameExistsAsync(request.NameEn, id, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Ingredient already exists." });
        }

        var section = await ResolveSectionAsync(request.ShopSectionSlug, cancellationToken);
        if (section is null)
        {
            return BadRequest(new ProblemDetails { Title = "Unknown shop section." });
        }

        var ingredient = await catalog.UpdateIngredientAsync(
            id,
            request.NameEn,
            request.NameDa,
            section,
            cancellationToken);
        return ingredient is null ? NotFound() : Ok(ToResponse(ingredient));
    }

    [HttpDelete("ingredients/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteIngredient(Guid id, CancellationToken cancellationToken) =>
        await catalog.DeleteIngredientAsync(id, cancellationToken) switch
        {
            IngredientDeleteResult.Deleted => NoContent(),
            IngredientDeleteResult.InUse => Conflict(
                new ProblemDetails { Title = "Ingredient is used by a recipe." }),
            _ => NotFound()
        };

    private async Task<string?> ResolveSectionAsync(string? slug, CancellationToken cancellationToken)
    {
        var wanted = string.IsNullOrWhiteSpace(slug) ? ShopSection.DefaultSlug : slug.Trim();
        var sections = await catalog.GetShopSectionsAsync(cancellationToken);
        return sections.Any(section => section.Slug == wanted) ? wanted : null;
    }

    private static IngredientResponse ToResponse(Ingredient ingredient) =>
        new(ingredient.Id, ingredient.NameEn, ingredient.NameDa, ingredient.ShopSectionSlug);
}
