CREATE TABLE "ShopSections" (
    "Slug" character varying(20) NOT NULL,
    "NameEn" character varying(80) NOT NULL,
    "NameDa" character varying(80) NOT NULL,
    "Position" integer NOT NULL,
    "Perishable" boolean NOT NULL,
    CONSTRAINT "PK_ShopSections" PRIMARY KEY ("Slug")
);

INSERT INTO "ShopSections" ("Slug", "NameEn", "NameDa", "Position", "Perishable") VALUES
    ('nonfood', 'Non-food', 'Non-food', 1, false),
    ('bread', 'Bread', 'Brød', 2, true),
    ('produce', 'Fruit and vegetables', 'Frugt og grønt', 3, true),
    ('fresh', 'Fresh produce', 'Ferskvarer', 4, true),
    ('meat_fish', 'Meat and fish', 'Kød og fisk', 5, true),
    ('frozen', 'Frozen products', 'Frost', 6, false),
    ('dairy', 'Dairy', 'Mejeri', 7, true),
    ('dry_goods', 'Dry goods', 'Kolonial', 8, false),
    ('canned', 'Canned goods', 'Konserves', 9, false);

ALTER TABLE "Ingredients"
    ADD COLUMN "ShopSectionSlug" character varying(20) NOT NULL DEFAULT 'dry_goods';

ALTER TABLE "Ingredients"
    ADD CONSTRAINT "FK_Ingredients_ShopSections" FOREIGN KEY ("ShopSectionSlug")
    REFERENCES "ShopSections" ("Slug") ON DELETE RESTRICT;

CREATE INDEX "IX_Ingredients_ShopSectionSlug" ON "Ingredients" ("ShopSectionSlug");
