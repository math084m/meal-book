using MealBook.Domain.Recipes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class AppUser : IdentityUser
{
}

public sealed class RecipeDbContext(DbContextOptions<RecipeDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole, string>(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeCookLog> RecipeCookLogs => Set<RecipeCookLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var recipe = modelBuilder.Entity<Recipe>();
        recipe.ToTable("Recipes");
        recipe.HasKey(item => item.Id);
        recipe.Property(item => item.Name).HasMaxLength(200).IsRequired();
        recipe.Property(item => item.Description).HasMaxLength(2000);
        recipe.Property(item => item.MealType).HasMaxLength(80).IsRequired();
        recipe.Property(item => item.Course).HasMaxLength(80).IsRequired();
        recipe.Property(item => item.CookingMethod).HasMaxLength(100).IsRequired();
        recipe.Property(item => item.Cuisine).HasMaxLength(100).IsRequired();

        recipe.HasMany(item => item.Ingredients)
            .WithOne()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        recipe.HasMany(item => item.Steps)
            .WithOne()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        recipe.HasMany(item => item.Equipment)
            .WithOne()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        recipe.HasMany(item => item.Tags)
            .WithOne()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        var ingredient = modelBuilder.Entity<RecipeIngredient>();
        ingredient.ToTable("RecipeIngredientEntity");
        ingredient.HasKey(item => item.Id);
        ingredient.Property(item => item.Name).HasMaxLength(200).IsRequired();
        ingredient.Property(item => item.Unit).HasMaxLength(80).IsRequired();
        ingredient.Property(item => item.Notes).HasMaxLength(500);
        ingredient.Property(item => item.Quantity).HasPrecision(12, 3);

        var step = modelBuilder.Entity<RecipeStep>();
        step.ToTable("RecipeStepEntity");
        step.HasKey(item => item.Id);
        step.Property(item => item.Instructions).HasMaxLength(4000).IsRequired();

        var equipment = modelBuilder.Entity<RecipeEquipment>();
        equipment.ToTable("RecipeEquipmentEntity");
        equipment.HasKey(item => item.Id);
        equipment.Property(item => item.Name).HasMaxLength(200).IsRequired();

        var tag = modelBuilder.Entity<RecipeTag>();
        tag.ToTable("RecipeTagEntity");
        tag.HasKey(item => item.Id);
        tag.Property(item => item.Name).HasMaxLength(80).IsRequired();
        tag.HasIndex(item => new { item.RecipeId, item.Name }).IsUnique();

        var cookLog = modelBuilder.Entity<RecipeCookLog>();
        cookLog.ToTable("RecipeCookLog");
        cookLog.HasKey(item => item.Id);
        cookLog.Property(item => item.UserId).HasMaxLength(450).IsRequired();
        cookLog.Property(item => item.Notes).HasMaxLength(1000);
        cookLog.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        cookLog.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        cookLog.HasIndex(item => new { item.UserId, item.CookedAt });
        cookLog.HasIndex(item => item.RecipeId);
    }
}
