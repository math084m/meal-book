CREATE TABLE "MealPlanEntries" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Servings" integer NOT NULL,
    "AddedByUserId" character varying(450) NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MealPlanEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MealPlanEntries_Recipes" FOREIGN KEY ("RecipeId")
        REFERENCES "Recipes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_MealPlanEntries_Servings" CHECK ("Servings" BETWEEN 1 AND 100)
);

CREATE UNIQUE INDEX "IX_MealPlanEntries_RecipeId" ON "MealPlanEntries" ("RecipeId");

CREATE TABLE "ShoppingChecks" (
    "IngredientId" uuid NOT NULL,
    "Unit" character varying(40) NOT NULL,
    "CheckedByUserId" character varying(450) NOT NULL,
    "CheckedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ShoppingChecks" PRIMARY KEY ("IngredientId", "Unit"),
    CONSTRAINT "FK_ShoppingChecks_Ingredients" FOREIGN KEY ("IngredientId")
        REFERENCES "Ingredients" ("Id") ON DELETE CASCADE
);
