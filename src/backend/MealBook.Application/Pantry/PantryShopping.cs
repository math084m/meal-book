using MealBook.Application.Planning;

namespace MealBook.Application.Pantry;

public static class PantryShopping
{
    public static MealPlanResponse Apply(MealPlanResponse plan, PantryResponse pantry)
    {
        if (!pantry.UseForShopping)
        {
            return plan;
        }

        var items = pantry.Items.ToDictionary(item => item.IngredientId);
        return plan with
        {
            UsePantry = true,
            Sections = plan.Sections.Select(section => section with
            {
                Items = section.Items.Select(item =>
                    item.CustomId is null && item.IngredientId is { } id && items.TryGetValue(id, out var stock)
                        ? item with
                        {
                            Have = Math.Round(item.Quantity * (stock.HaveFraction ?? 1m), 3),
                            PantryItem = true,
                            PantryOverride = stock.HaveFraction is not null
                        }
                        : item).ToArray()
            }).ToArray()
        };
    }
}
