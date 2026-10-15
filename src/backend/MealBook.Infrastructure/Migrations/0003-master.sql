ALTER TABLE "Recipes"
    ADD COLUMN "IsPublished" boolean NOT NULL DEFAULT true,
    ADD COLUMN "SourceUrl" character varying(2048);

CREATE INDEX "IX_Recipes_IsPublished_AddedAt"
    ON "Recipes" ("IsPublished", "AddedAt");
