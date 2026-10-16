-- Second ingredient cleanup pass: merge synonym pairs that became visible once the disambiguating
-- Danish suffixes were removed, and fix swede (kålroe, not kohlrabi).

CREATE TEMP TABLE "IngredientMerge" ("Loser" text NOT NULL, "Winner" text NOT NULL);

INSERT INTO "IngredientMerge" ("Loser", "Winner") VALUES
    ('Eggplant', 'Aubergine'),
    ('Chili powder', 'Chilli powder'),
    ('Pickled gherkins', 'Cornichons'),
    ('Fresh cilantro', 'Fresh coriander'),
    ('Sweet chili sauce', 'Sweet chilli sauce'),
    ('Pistachios, shelled', 'Pistachios'),
    ('Mini marshmallows', 'Marshmallows'),
    ('Fried shallots, Vietnamese', 'Fried shallots'),
    ('Dried scallops, Chinese', 'Dried scallops'),
    ('Vermicelli rice noodles', 'Vermicelli, rice'),
    ('Chinese cabbage', 'Wombok'),
    ('Bacon lardons', 'Lardons'),
    ('Fava beans', 'Broad beans'),
    ('Chanterelles', 'Girolles'),
    ('Coquilles Saint-Jacques', 'Scallops'),
    ('Morning glory', 'Water spinach'),
    ('Mexican cinnamon', 'Canela'),
    ('Mâche', 'Lamb''s lettuce');

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

UPDATE "Ingredients" SET "NameDa" = 'Kålroe' WHERE lower("NameEn") = 'swede';
