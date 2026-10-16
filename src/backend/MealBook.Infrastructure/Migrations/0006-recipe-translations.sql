ALTER TABLE "Recipes"
    ADD COLUMN "Language" character varying(2) NOT NULL DEFAULT 'en';

CREATE TABLE "RecipeTranslations" (
    "RecipeId" uuid NOT NULL,
    "Language" character varying(2) NOT NULL,
    "Content" text NOT NULL,
    "SourceHash" character varying(64) NOT NULL,
    "TranslatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RecipeTranslations" PRIMARY KEY ("RecipeId", "Language"),
    CONSTRAINT "FK_RecipeTranslations_Recipes_RecipeId"
        FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE CASCADE
);
