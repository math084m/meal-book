using MealBook.Domain.Recipes;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class AppUser : IdentityUser
{
}

public sealed class RecipeDbContext(DbContextOptions<RecipeDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole, string>(options), IDataProtectionKeyContext
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeCookLog> RecipeCookLogs => Set<RecipeCookLog>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var recipe = modelBuilder.Entity<Recipe>();
        recipe.ToTable("Recipes");
        recipe.HasKey(item => item.Id);
        recipe.Property(item => item.Name).HasMaxLength(200).IsRequired();
        recipe.Property(item => item.Description).HasMaxLength(2000);
        recipe.Property(item => item.IsPublished).IsRequired();
        recipe.Property(item => item.SourceUrl).HasMaxLength(2048);
        recipe.HasIndex(item => new { item.IsPublished, item.AddedAt });

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
        recipe.HasMany(item => item.Terms)
            .WithOne()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        recipe.Property(item => item.Language).HasMaxLength(2).IsRequired();
        recipe.Property(item => item.ImageUpdatedAt);

        var translation = modelBuilder.Entity<RecipeTranslation>();
        translation.ToTable("RecipeTranslations");
        translation.HasKey(item => new { item.RecipeId, item.Language });
        translation.Property(item => item.Language).HasMaxLength(2);
        translation.Property(item => item.Content).HasColumnType("text").IsRequired();
        translation.Property(item => item.SourceHash).HasMaxLength(64).IsRequired();
        translation.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        var ingredient = modelBuilder.Entity<RecipeIngredient>();
        ingredient.ToTable("RecipeIngredientEntity");
        ingredient.HasKey(item => item.Id);
        ingredient.Property(item => item.Name).HasMaxLength(200).IsRequired();
        ingredient.Property(item => item.Unit).HasColumnType("text").IsRequired();
        ingredient.Property(item => item.Section).HasColumnType("text");
        ingredient.Property(item => item.Notes).HasMaxLength(500);
        ingredient.Property(item => item.Quantity).HasPrecision(12, 3);
        ingredient.HasOne<Ingredient>()
            .WithMany()
            .HasForeignKey(item => item.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        var catalogIngredient = modelBuilder.Entity<Ingredient>();
        catalogIngredient.ToTable("Ingredients");
        catalogIngredient.HasKey(item => item.Id);
        catalogIngredient.Property(item => item.NameEn).HasMaxLength(200).IsRequired();
        catalogIngredient.Property(item => item.NameDa).HasMaxLength(200).IsRequired();
        catalogIngredient.Property(item => item.ShopSectionSlug).HasMaxLength(20).IsRequired();
        catalogIngredient.HasOne<ShopSection>()
            .WithMany()
            .HasForeignKey(item => item.ShopSectionSlug)
            .OnDelete(DeleteBehavior.Restrict);

        var shopSection = modelBuilder.Entity<ShopSection>();
        shopSection.ToTable("ShopSections");
        shopSection.HasKey(item => item.Slug);
        shopSection.Property(item => item.Slug).HasMaxLength(20);
        shopSection.Property(item => item.NameEn).HasMaxLength(80).IsRequired();
        shopSection.Property(item => item.NameDa).HasMaxLength(80).IsRequired();

        var term = modelBuilder.Entity<Term>();
        term.ToTable("Terms");
        term.HasKey(item => new { item.Kind, item.Slug });
        term.Property(item => item.Kind).HasMaxLength(40);
        term.Property(item => item.Slug).HasMaxLength(80);
        term.Property(item => item.NameEn).HasMaxLength(120).IsRequired();
        term.Property(item => item.NameDa).HasMaxLength(120).IsRequired();

        var recipeTerm = modelBuilder.Entity<RecipeTerm>();
        recipeTerm.ToTable("RecipeTerms");
        recipeTerm.HasKey(item => new { item.RecipeId, item.Kind, item.Slug });
        recipeTerm.Property(item => item.Kind).HasMaxLength(40);
        recipeTerm.Property(item => item.Slug).HasMaxLength(80);
        recipeTerm.HasOne<Term>()
            .WithMany()
            .HasForeignKey(item => new { item.Kind, item.Slug })
            .OnDelete(DeleteBehavior.Restrict);
        recipeTerm.HasIndex(item => new { item.Kind, item.Slug });

        var step = modelBuilder.Entity<RecipeStep>();
        step.ToTable("RecipeStepEntity");
        step.HasKey(item => item.Id);
        step.Property(item => item.Instructions).HasMaxLength(4000).IsRequired();

        var equipment = modelBuilder.Entity<RecipeEquipment>();
        equipment.ToTable("RecipeEquipmentEntity");
        equipment.HasKey(item => item.Id);
        equipment.Property(item => item.Name).HasMaxLength(200).IsRequired();

        var cookLog =  modelBuilder.Entity<RecipeCookLog>();
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
