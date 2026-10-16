using MealBook.Application.Planning;
using MealBook.Domain.Planning;
using MealBook.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class MealPlanRepository(RecipeDbContext db) : IMealPlanRepository
{
    public async Task<IReadOnlyList<PlanSourceEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        var entries = await (
            from entry in db.Set<MealPlanEntry>().AsNoTracking()
            join recipe in db.Recipes.AsNoTracking() on entry.RecipeId equals recipe.Id
            select new
            {
                entry.Id,
                entry.RecipeId,
                recipe.Name,
                RecipeServings = recipe.Servings,
                entry.Servings,
                recipe.ImageUpdatedAt,
                entry.AddedAt,
                entry.PlannedStart,
                entry.PlannedDays
            }).ToListAsync(cancellationToken);
        if (entries.Count == 0)
        {
            return [];
        }

        var recipeIds = entries.Select(entry => entry.RecipeId).ToList();
        var lines = await (
            from line in db.Set<RecipeIngredient>().AsNoTracking()
            where recipeIds.Contains(line.RecipeId)
            join catalog in db.Set<Ingredient>().AsNoTracking() on line.IngredientId equals (Guid?)catalog.Id into joined
            from catalog in joined.DefaultIfEmpty()
            select new
            {
                line.RecipeId,
                line.IngredientId,
                line.Name,
                line.Quantity,
                line.Unit,
                CatalogEn = catalog == null ? null : catalog.NameEn,
                CatalogDa = catalog == null ? null : catalog.NameDa,
                Section = catalog == null ? ShopSection.DefaultSlug : catalog.ShopSectionSlug
            }).ToListAsync(cancellationToken);

        var byRecipe = lines.ToLookup(line => line.RecipeId);
        return entries
            .Select(entry => new PlanSourceEntry(
                entry.Id,
                entry.RecipeId,
                entry.Name,
                entry.RecipeServings,
                entry.Servings,
                entry.ImageUpdatedAt,
                entry.AddedAt,
                byRecipe[entry.RecipeId]
                    .Select(line => new PlanSourceLine(
                        line.IngredientId,
                        line.CatalogEn ?? line.Name,
                        line.CatalogDa ?? string.Empty,
                        line.Section,
                        line.Quantity,
                        line.Unit))
                    .ToArray(),
                entry.PlannedStart,
                entry.PlannedDays))
            .ToArray();
    }

    public async Task<IReadOnlySet<ShoppingCheckKey>> GetChecksAsync(CancellationToken cancellationToken) =>
        (await db.Set<ShoppingCheck>()
            .AsNoTracking()
            .Select(check => new { check.IngredientId, check.Unit })
            .ToListAsync(cancellationToken))
        .Select(check => new ShoppingCheckKey(check.IngredientId, check.Unit))
        .ToHashSet();

    public async Task<IReadOnlyDictionary<ShoppingCheckKey, decimal>> GetHavesAsync(
        CancellationToken cancellationToken) =>
        (await db.Set<ShoppingHave>()
            .AsNoTracking()
            .Select(have => new { have.IngredientId, have.Unit, have.Quantity })
            .ToListAsync(cancellationToken))
        .ToDictionary(have => new ShoppingCheckKey(have.IngredientId, have.Unit), have => have.Quantity);

    public async Task SetHaveAsync(
        Guid ingredientId,
        string unit,
        decimal quantity,
        string userId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            await db.Set<ShoppingHave>()
                .Where(have => have.IngredientId == ingredientId && have.Unit == unit)
                .ExecuteDeleteAsync(cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ShoppingHaves" ("IngredientId", "Unit", "Quantity", "UpdatedByUserId", "UpdatedAt")
            SELECT {ingredientId}, {unit}, {quantity}, {userId}, {now}
            WHERE EXISTS (SELECT 1 FROM "Ingredients" WHERE "Id" = {ingredientId})
            ON CONFLICT ("IngredientId", "Unit")
            DO UPDATE SET "Quantity" = EXCLUDED."Quantity", "UpdatedByUserId" = EXCLUDED."UpdatedByUserId", "UpdatedAt" = EXCLUDED."UpdatedAt"
            """,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingCustomItem>> GetCustomItemsAsync(CancellationToken cancellationToken) =>
        await db.Set<ShoppingCustomItem>().AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddCustomItemAsync(
        string name,
        decimal quantity,
        string unit,
        string sectionSlug,
        string userId,
        CancellationToken cancellationToken)
    {
        db.Set<ShoppingCustomItem>().Add(ShoppingCustomItem.Create(name, quantity, unit, sectionSlug, userId));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SetCustomItemCheckedAsync(Guid id, bool isChecked, CancellationToken cancellationToken) =>
        await db.Set<ShoppingCustomItem>()
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsChecked, isChecked), cancellationToken) > 0;

    public async Task<bool> RemoveCustomItemAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Set<ShoppingCustomItem>().Where(item => item.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<IReadOnlyList<PerishableUsage>> GetPerishableUsagesAsync(CancellationToken cancellationToken) =>
        await (
            from line in db.Set<RecipeIngredient>().AsNoTracking()
            join recipe in db.Recipes.AsNoTracking() on line.RecipeId equals recipe.Id
            join catalog in db.Set<Ingredient>().AsNoTracking() on line.IngredientId equals (Guid?)catalog.Id
            join section in db.Set<ShopSection>().AsNoTracking() on catalog.ShopSectionSlug equals section.Slug
            where recipe.IsPublished && section.Perishable
            select new PerishableUsage(
                recipe.Id,
                recipe.Name,
                recipe.ImageUpdatedAt,
                catalog.Id,
                catalog.NameEn,
                catalog.NameDa)).ToListAsync(cancellationToken);

    public Task<int> CountPublishedRecipesAsync(CancellationToken cancellationToken) =>
        db.Recipes.CountAsync(recipe => recipe.IsPublished, cancellationToken);

    public async Task<PlanChangeResult> AddAsync(
        Guid recipeId,
        int? servings,
        string userId,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes
            .AsNoTracking()
            .Where(item => item.Id == recipeId && item.IsPublished)
            .Select(item => new { item.Servings })
            .SingleOrDefaultAsync(cancellationToken);
        if (recipe is null)
        {
            return PlanChangeResult.NotFound;
        }

        var existing = await db.Set<MealPlanEntry>()
            .SingleOrDefaultAsync(entry => entry.RecipeId == recipeId, cancellationToken);
        if (existing is not null)
        {
            if (servings is { } wanted)
            {
                existing.SetServings(wanted);
                await db.SaveChangesAsync(cancellationToken);
            }

            return PlanChangeResult.Changed;
        }

        db.Set<MealPlanEntry>().Add(MealPlanEntry.Create(recipeId, servings ?? recipe.Servings, userId));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request added the same recipe at the same moment; the plan already contains it.
            db.ChangeTracker.Clear();
        }

        return PlanChangeResult.Changed;
    }

    public async Task<PlanChangeResult> SetServingsAsync(
        Guid recipeId,
        int servings,
        CancellationToken cancellationToken)
    {
        var entry = await db.Set<MealPlanEntry>()
            .SingleOrDefaultAsync(item => item.RecipeId == recipeId, cancellationToken);
        if (entry is null)
        {
            return PlanChangeResult.NotFound;
        }

        entry.SetServings(servings);
        await db.SaveChangesAsync(cancellationToken);
        return PlanChangeResult.Changed;
    }

    public async Task<PlanChangeResult> ScheduleAsync(
        Guid recipeId,
        DateOnly? start,
        int days,
        CancellationToken cancellationToken)
    {
        var entry = await db.Set<MealPlanEntry>()
            .SingleOrDefaultAsync(item => item.RecipeId == recipeId, cancellationToken);
        if (entry is null)
        {
            return PlanChangeResult.NotFound;
        }

        entry.Schedule(start, days);
        await db.SaveChangesAsync(cancellationToken);
        return PlanChangeResult.Changed;
    }

    public async Task<PlanChangeResult> RemoveAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        var removed = await db.Set<MealPlanEntry>()
            .Where(entry => entry.RecipeId == recipeId)
            .ExecuteDeleteAsync(cancellationToken);
        if (removed == 0)
        {
            return PlanChangeResult.NotFound;
        }

        if (!await db.Set<MealPlanEntry>().AnyAsync(cancellationToken))
        {
            await ClearChecksAsync(cancellationToken);
        }

        return PlanChangeResult.Changed;
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await db.Set<MealPlanEntry>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<ShoppingCustomItem>().ExecuteDeleteAsync(cancellationToken);
        await ClearChecksAsync(cancellationToken);
    }

    public async Task SetCheckedAsync(
        Guid ingredientId,
        string unit,
        bool isChecked,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!isChecked)
        {
            await db.Set<ShoppingCheck>()
                .Where(check => check.IngredientId == ingredientId && check.Unit == unit)
                .ExecuteDeleteAsync(cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ShoppingChecks" ("IngredientId", "Unit", "CheckedByUserId", "CheckedAt")
            SELECT {ingredientId}, {unit}, {userId}, {now}
            WHERE EXISTS (SELECT 1 FROM "Ingredients" WHERE "Id" = {ingredientId})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken);
    }

    public async Task ClearChecksAsync(CancellationToken cancellationToken)
    {
        await db.Set<ShoppingCheck>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<ShoppingHave>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<ShoppingCustomItem>().ExecuteUpdateAsync(
            setters => setters.SetProperty(item => item.IsChecked, false),
            cancellationToken);
    }
}

public sealed class PgMealPlanNotifier(RecipeDbContext db) : IMealPlanNotifier
{
    public const string Channel = "meal_plan_changed";

    public async Task PlanChangedAsync(CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_notify('{Channel}', '')", cancellationToken);
}
