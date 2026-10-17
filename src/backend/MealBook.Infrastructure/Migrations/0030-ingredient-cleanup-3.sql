-- Third ingredient cleanup pass: ingredients that shared a Danish name showed up as separate,
-- identical-looking lines on the Danish shopping list. True synonyms are merged; the rest get a
-- correct Danish name.

CREATE TEMP TABLE "IngredientMerge" ("Loser" text NOT NULL, "Winner" text NOT NULL);

INSERT INTO "IngredientMerge" ("Loser", "Winner") VALUES
    ('Tomato puree', 'Tomato paste'),
    ('Canned tomatoes', 'Chopped tomatoes'),
    ('Cilantro', 'Coriander'),
    ('Jambon blanc', 'Cooked ham');

DELETE FROM "IngredientMerge" m
WHERE NOT EXISTS (SELECT 1 FROM "Ingredients" i WHERE lower(i."NameEn") = lower(m."Loser"))
   OR NOT EXISTS (SELECT 1 FROM "Ingredients" i WHERE lower(i."NameEn") = lower(m."Winner"));

UPDATE "RecipeIngredientEntity" r
SET "IngredientId" = w."Id", "Name" = w."NameEn"
FROM "IngredientMerge" m
JOIN "Ingredients" l ON lower(l."NameEn") = lower(m."Loser")
JOIN "Ingredients" w ON lower(w."NameEn") = lower(m."Winner")
WHERE r."IngredientId" = l."Id";

DELETE FROM "Ingredients" i
USING "IngredientMerge" m
WHERE lower(i."NameEn") = lower(m."Loser");

DROP TABLE "IngredientMerge";

UPDATE "Ingredients" SET "NameDa" = 'Escarole' WHERE lower("NameEn") = 'escarole';
UPDATE "Ingredients" SET "NameDa" = 'Kokosmel (fint mel)' WHERE lower("NameEn") = 'coconut flour';
UPDATE "Ingredients" SET "NameDa" = 'Okseskank' WHERE lower("NameEn") = 'beef shin';
UPDATE "Ingredients" SET "NameDa" = 'Svinekam' WHERE lower("NameEn") = 'pork loin';
UPDATE "Ingredients" SET "NameDa" = 'Svinebov' WHERE lower("NameEn") = 'pork shoulder';
