namespace MealBook.Application.Planning;

// Merges compatible units so that "1 dl" and "2 tbsp" of the same ingredient become one shopping line.
public static class PlanUnits
{
    private static readonly Dictionary<string, decimal> MillilitresPerUnit = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pinch"] = 0.3125m,
        ["pinches"] = 0.3125m,
        ["knivspids"] = 0.3125m,
        ["dash"] = 0.625m,
        ["ml"] = 1m,
        ["cl"] = 10m,
        ["dl"] = 100m,
        ["l"] = 1000m,
        ["tsp"] = 5m,
        ["teaspoon"] = 5m,
        ["teaspoons"] = 5m,
        ["tsk"] = 5m,
        ["tbsp"] = 15m,
        ["tablespoon"] = 15m,
        ["tablespoons"] = 15m,
        ["spsk"] = 15m
    };

    private static readonly Dictionary<string, decimal> GramsPerUnit = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = 1m,
        ["kg"] = 1000m
    };

    public static (decimal Quantity, string Unit) Normalize(decimal quantity, string unit)
    {
        var key = unit.Trim();
        if (MillilitresPerUnit.TryGetValue(key, out var ml))
        {
            return (quantity * ml, "ml");
        }

        if (GramsPerUnit.TryGetValue(key, out var grams))
        {
            return (quantity * grams, "g");
        }

        return (quantity, key.ToLowerInvariant());
    }
}
