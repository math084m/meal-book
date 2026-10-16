CREATE TABLE "RecipeFavorites" (
    "UserId" character varying(450) NOT NULL,
    "RecipeId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RecipeFavorites" PRIMARY KEY ("UserId", "RecipeId"),
    CONSTRAINT "FK_RecipeFavorites_Recipes" FOREIGN KEY ("RecipeId")
        REFERENCES "Recipes" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RecipeFavorites_RecipeId" ON "RecipeFavorites" ("RecipeId");
