ALTER TABLE "AspNetUsers" ADD COLUMN "UsePantryForShopping" boolean NOT NULL DEFAULT false;

CREATE TABLE "PantryItems" (
    "UserId" character varying(450) NOT NULL,
    "IngredientId" uuid NOT NULL,
    "HaveFraction" numeric(4,3) NULL,
    CONSTRAINT "PK_PantryItems" PRIMARY KEY ("UserId", "IngredientId"),
    CONSTRAINT "FK_PantryItems_AspNetUsers" FOREIGN KEY ("UserId")
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PantryItems_Ingredients" FOREIGN KEY ("IngredientId")
        REFERENCES "Ingredients" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_PantryItems_HaveFraction" CHECK ("HaveFraction" BETWEEN 0 AND 1)
);

CREATE INDEX "IX_PantryItems_IngredientId" ON "PantryItems" ("IngredientId");
