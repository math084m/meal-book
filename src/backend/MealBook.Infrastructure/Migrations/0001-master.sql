-- Create the initial recipe library schema.

CREATE TABLE "Recipes" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(2000),
    "Servings" integer NOT NULL,
    "PrepTimeMinutes" integer NOT NULL,
    "CookTimeMinutes" integer NOT NULL,
    CONSTRAINT "PK_Recipes" PRIMARY KEY ("Id")
);

CREATE TABLE "RecipeEquipmentEntity" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Position" integer NOT NULL,
    CONSTRAINT "PK_RecipeEquipmentEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RecipeEquipmentEntity_Recipes_RecipeId"
        FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "RecipeIngredientEntity" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Quantity" numeric(12, 3) NOT NULL,
    "Unit" character varying(80) NOT NULL,
    "Notes" character varying(500),
    "Position" integer NOT NULL,
    CONSTRAINT "PK_RecipeIngredientEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RecipeIngredientEntity_Recipes_RecipeId"
        FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "RecipeStepEntity" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Instructions" character varying(4000) NOT NULL,
    "Position" integer NOT NULL,
    CONSTRAINT "PK_RecipeStepEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RecipeStepEntity_Recipes_RecipeId"
        FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RecipeEquipmentEntity_RecipeId" ON "RecipeEquipmentEntity" ("RecipeId");
CREATE INDEX "IX_RecipeIngredientEntity_RecipeId" ON "RecipeIngredientEntity" ("RecipeId");
CREATE INDEX "IX_RecipeStepEntity_RecipeId" ON "RecipeStepEntity" ("RecipeId");
