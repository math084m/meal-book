using MealBook.Application.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MealBook.Api.Controllers;

// Turns unknown term or ingredient references into a 400 response.
[AttributeUsage(AttributeTargets.Class)]
public sealed class RecipeValidationFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is RecipeValidationException exception)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Invalid recipe.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
            context.ExceptionHandled = true;
        }
    }
}
