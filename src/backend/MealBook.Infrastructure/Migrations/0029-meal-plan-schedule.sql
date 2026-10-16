ALTER TABLE "MealPlanEntries" ADD COLUMN "PlannedStart" date NULL;
ALTER TABLE "MealPlanEntries" ADD COLUMN "PlannedDays" integer NOT NULL DEFAULT 1;
ALTER TABLE "MealPlanEntries" ADD CONSTRAINT "CK_MealPlanEntries_PlannedDays" CHECK ("PlannedDays" BETWEEN 1 AND 14);
