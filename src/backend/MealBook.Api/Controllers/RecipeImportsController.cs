using System.Security.Cryptography;
using System.Text;
using MealBook.Application.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealBook.Api.Controllers;

[ApiController]
[Route("api/recipe-imports")]
public sealed class RecipeImportsController(
    IRecipeService recipes,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RecipeResponse>> Import(
        ImportRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var expectedKey = configuration["RecipeImport:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey) || expectedKey.Length < 32)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Title = "Recipe import is not configured." });
        }

        if (!Request.Headers.TryGetValue("X-Recipe-Import-Key", out var suppliedKey)
            || suppliedKey.Count != 1
            || !FixedTimeEquals(expectedKey, suppliedKey[0]))
        {
            return Unauthorized();
        }

        if (!Uri.TryCreate(request.SourceUrl, UriKind.Absolute, out var sourceUri)
            || sourceUri.Scheme is not ("http" or "https"))
        {
            ModelState.AddModelError(nameof(request.SourceUrl), "Source URL must use HTTP or HTTPS.");
            return ValidationProblem(ModelState);
        }

        var recipe = await recipes.ImportAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, recipe);
    }

    private static bool FixedTimeEquals(string expected, string? supplied)
    {
        if (string.IsNullOrEmpty(supplied))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(supplied));
    }
}
