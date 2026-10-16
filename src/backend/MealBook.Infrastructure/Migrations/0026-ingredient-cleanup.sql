-- Ingredient catalog cleanup: merge English synonyms that share a Danish name (they split shopping-list
-- lines), fix untranslated Danish names and strip clutter suffixes.

CREATE TEMP TABLE "IngredientMerge" ("Loser" text NOT NULL, "Winner" text NOT NULL);

INSERT INTO "IngredientMerge" ("Loser", "Winner") VALUES
    ('Celery stalks', 'Celery'),
    ('Celery stalk', 'Celery'),
    ('Stalk Celery', 'Celery'),
    ('Bulgur wheat', 'Bulgur'),
    ('Champignons de Paris', 'Mushrooms'),
    ('Red pepper flakes', 'Chili flakes'),
    ('Danbo cheese', 'Danbo'),
    ('Powdered sugar', 'Icing sugar'),
    ('Whole wheat flour', 'Wholemeal flour'),
    ('Chèvre', 'Goat cheese'),
    ('Linseed', 'Flax seeds'),
    ('Potato flour', 'Potato starch'),
    ('Sticky rice flour', 'Glutinous rice flour'),
    ('Corn', 'Sweetcorn'),
    ('Cornflour', 'Cornstarch'),
    ('Moules', 'Mussels'),
    ('Liver pate', 'Leverpostej'),
    ('Beef fillet', 'Beef tenderloin'),
    ('Parsnips', 'Parsnip'),
    ('Whipping cream', 'Heavy cream'),
    ('Rocket', 'Arugula'),
    ('Shallots', 'Shallot'),
    ('Pork cutlet', 'Pork chop'),
    ('Spring onions', 'Spring onion'),
    ('Egg', 'Eggs'),
    ('Tomato Purée', 'Tomato puree'),
    ('Tomato Passata', 'Passata'),
    ('Ground allspice', 'Allspice, ground'),
    ('Pork mince', 'Minced pork'),
    ('Peanut oil', 'Groundnut oil');

-- Only merge when both sides exist, so a catalog that was edited by hand is left alone.
DELETE FROM "IngredientMerge" m
WHERE NOT EXISTS (SELECT 1 FROM "Ingredients" i WHERE lower(i."NameEn") = lower(m."Loser"))
   OR NOT EXISTS (SELECT 1 FROM "Ingredients" i WHERE lower(i."NameEn") = lower(m."Winner"));

UPDATE "RecipeIngredientEntity" r
SET "IngredientId" = w."Id", "Name" = w."NameEn"
FROM "IngredientMerge" m
JOIN "Ingredients" l ON lower(l."NameEn") = lower(m."Loser")
JOIN "Ingredients" w ON lower(w."NameEn") = lower(m."Winner")
WHERE r."IngredientId" = l."Id";

-- Shopping checks and haves for a merged ingredient are transient and cascade away with it.
DELETE FROM "Ingredients" i
USING "IngredientMerge" m
WHERE lower(i."NameEn") = lower(m."Loser");

DROP TABLE "IngredientMerge";

-- Danish names that were still English or carried a disambiguating suffix.
UPDATE "Ingredients" SET "NameDa" = 'Laurbærblade' WHERE lower("NameEn") = 'bay leaves';
UPDATE "Ingredients" SET "NameDa" = 'Hvidt løg' WHERE lower("NameEn") = 'white onion';
UPDATE "Ingredients" SET "NameDa" = 'Sød chilisauce' WHERE lower("NameEn") = 'sweet chilli sauce';
UPDATE "Ingredients" SET "NameDa" = 'Mexicansk kanel' WHERE lower("NameEn") = 'canela';
UPDATE "Ingredients" SET "NameDa" = 'Tomatpuré' WHERE lower("NameEn") = 'tomato puree';
UPDATE "Ingredients" SET "NameDa" = 'Malet allehånde' WHERE lower("NameEn") = 'allspice, ground';
UPDATE "Ingredients" SET "NameDa" = 'Hakket svinekød' WHERE lower("NameEn") = 'minced pork';
UPDATE "Ingredients" SET "NameDa" = 'Jordnøddeolie' WHERE lower("NameEn") = 'groundnut oil';

UPDATE "Ingredients" SET "NameDa" = 'Aubergine' WHERE "NameDa" = 'Aubergine (stor)';
UPDATE "Ingredients" SET "NameDa" = 'Oksekæber' WHERE "NameDa" = 'Oksekæber (braiseret)';
UPDATE "Ingredients" SET "NameDa" = 'Blåmuslinger' WHERE "NameDa" = 'Blåmuslinger (danske)';
UPDATE "Ingredients" SET "NameDa" = 'Hestebønner' WHERE "NameDa" = 'Hestebønner (store)';
UPDATE "Ingredients" SET "NameDa" = 'Chilipulver' WHERE "NameDa" = 'Chilipulver (mild)';
UPDATE "Ingredients" SET "NameDa" = 'Hakkede tomater' WHERE "NameDa" = 'Hakkede tomater (dåse)';
UPDATE "Ingredients" SET "NameDa" = 'Koriander' WHERE "NameDa" = 'Koriander (blade)';
UPDATE "Ingredients" SET "NameDa" = 'Frisk koriander' WHERE "NameDa" = 'Frisk koriander (cilantro)';
UPDATE "Ingredients" SET "NameDa" = 'Cornichoner' WHERE "NameDa" = 'Cornichoner (små)';
UPDATE "Ingredients" SET "NameDa" = 'Pandekagedej' WHERE "NameDa" = 'Pandekagedej (crêpe)';
UPDATE "Ingredients" SET "NameDa" = 'Kokosmel' WHERE "NameDa" = 'Kokosmel (revet)';
UPDATE "Ingredients" SET "NameDa" = 'Endivie' WHERE "NameDa" = 'Endivie (belgisk)';
UPDATE "Ingredients" SET "NameDa" = 'Kålrabi' WHERE "NameDa" = 'Kålrabi (knold)';
UPDATE "Ingredients" SET "NameDa" = 'Baconterninger' WHERE "NameDa" = 'Baconterninger (franske)';
UPDATE "Ingredients" SET "NameDa" = 'Skumfiduser' WHERE "NameDa" = 'Skumfiduser (store)';
UPDATE "Ingredients" SET "NameDa" = 'Risvermicelli' WHERE "NameDa" = 'Risvermicelli (tynde)';
UPDATE "Ingredients" SET "NameDa" = 'Vandspinat' WHERE "NameDa" = 'Vandspinat (kangkong)';
UPDATE "Ingredients" SET "NameDa" = 'Kinakål' WHERE "NameDa" = 'Kinakål (wombok)';
UPDATE "Ingredients" SET "NameDa" = 'Kogt skinke' WHERE "NameDa" = 'Kogt skinke (fransk)';
UPDATE "Ingredients" SET "NameDa" = 'Kammuslinger' WHERE "NameDa" = 'Kammuslinger (Saint-Jacques)';
UPDATE "Ingredients" SET "NameDa" = 'Pistacienødder' WHERE "NameDa" = 'Pistacienødder (afskallede)';
UPDATE "Ingredients" SET "NameDa" = 'Kantareller' WHERE "NameDa" = 'Kantareller (franske)';
UPDATE "Ingredients" SET "NameDa" = 'Grønne chilier' WHERE "NameDa" = 'Grønne chilier (indiske)';
UPDATE "Ingredients" SET "NameDa" = 'Sprøde skalotteløg' WHERE "NameDa" = 'Sprøde skalotteløg (vietnamesiske)';
UPDATE "Ingredients" SET "NameDa" = 'Tørrede kammuslinger' WHERE "NameDa" = 'Tørrede kammuslinger (kinesiske)';
UPDATE "Ingredients" SET "NameDa" = 'Vårsalat' WHERE "NameDa" = 'Vårsalat (feldsalat)';

-- "Salt & Peber" was a Danish name stored as the English one.
UPDATE "Ingredients" SET "NameEn" = 'Salt and pepper', "NameDa" = 'Salt og peber'
WHERE "NameEn" = 'Salt & Peber'
  AND NOT EXISTS (SELECT 1 FROM "Ingredients" WHERE lower("NameEn") = 'salt and pepper');
