CREATE TABLE "ShoppingHaves" (
    "IngredientId" uuid NOT NULL,
    "Unit" character varying(40) NOT NULL,
    "Quantity" numeric(14, 3) NOT NULL,
    "UpdatedByUserId" character varying(450) NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ShoppingHaves" PRIMARY KEY ("IngredientId", "Unit"),
    CONSTRAINT "FK_ShoppingHaves_Ingredients" FOREIGN KEY ("IngredientId")
        REFERENCES "Ingredients" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_ShoppingHaves_Quantity" CHECK ("Quantity" > 0)
);
