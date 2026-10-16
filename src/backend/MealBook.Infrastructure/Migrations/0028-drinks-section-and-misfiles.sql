-- Alcohol and drinks had no shop section and were lumped into dry goods. Give them their own aisle and
-- move a handful of other misfiled items.

INSERT INTO "ShopSections" ("Slug", "NameEn", "NameDa", "Position", "Perishable")
VALUES ('drinks', 'Drinks and alcohol', 'Drikkevarer og alkohol', 10, false)
ON CONFLICT ("Slug") DO NOTHING;

UPDATE "Ingredients" SET "ShopSectionSlug" = 'drinks'
WHERE "ShopSectionSlug" = 'dry_goods' AND lower("NameEn") IN (
    'aperol', 'aquavit', 'arak', 'armagnac', 'amaretto', 'beer', 'beer, dark', 'beer, wheat', 'burgundy wine',
    'calvados', 'campari', 'chablis', 'champagne', 'cherry brandy', 'christmas beer', 'cider', 'cognac',
    'cointreau', 'crème de cassis', 'dry red wine', 'dry sherry', 'dry vermouth', 'dry white wine',
    'elderflower cordial', 'grand marnier', 'grapefruit soda', 'grappa', 'gløgg', 'kirsch', 'limoncello',
    'marsala', 'marsala wine', 'mexican beer', 'mezcal', 'orange juice', 'pastis', 'pernod', 'pinot noir',
    'pomegranate juice', 'port wine', 'prosecco', 'raki', 'red wine', 'sake', 'sherry', 'snaps',
    'sweet vermouth', 'tequila', 'triple sec', 'vin santo', 'white wine');

UPDATE "Ingredients" SET "ShopSectionSlug" = 'canned'
WHERE "ShopSectionSlug" = 'dry_goods' AND lower("NameEn") IN
    ('adobo sauce', 'jam', 'fig jam', 'jelly', 'lime pickle', 'mango pickle');

UPDATE "Ingredients" SET "ShopSectionSlug" = 'meat_fish'
WHERE "ShopSectionSlug" = 'dry_goods' AND lower("NameEn") IN
    ('bottarga', 'duck fat', 'goose fat', 'guanciale fat');

UPDATE "Ingredients" SET "ShopSectionSlug" = 'fresh'
WHERE "ShopSectionSlug" = 'dry_goods' AND lower("NameEn") = 'mochi';
