ALTER TABLE "AspNetUsers" ADD COLUMN "DisplayName" character varying(30) NULL;

-- Names are shown publicly next to comments, so they must be unique regardless of case.
CREATE UNIQUE INDEX "IX_AspNetUsers_DisplayName" ON "AspNetUsers" (lower("DisplayName"));

-- Threads are one level deep: "ParentId" always references a top-level comment.
-- A deleted comment that still has replies keeps its row with an empty body and "DeletedAt" set.
CREATE TABLE "RecipeComments" (
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "ParentId" uuid NULL,
    "UserId" character varying(450) NOT NULL,
    "Body" character varying(2000) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "DeletedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_RecipeComments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RecipeComments_Recipes" FOREIGN KEY ("RecipeId")
        REFERENCES "Recipes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RecipeComments_Parent" FOREIGN KEY ("ParentId")
        REFERENCES "RecipeComments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RecipeComments_AspNetUsers" FOREIGN KEY ("UserId")
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_RecipeComments_Body" CHECK ("DeletedAt" IS NOT NULL OR length(btrim("Body")) > 0)
);

CREATE INDEX "IX_RecipeComments_RecipeId_CreatedAt" ON "RecipeComments" ("RecipeId", "CreatedAt");
CREATE INDEX "IX_RecipeComments_ParentId" ON "RecipeComments" ("ParentId");
CREATE INDEX "IX_RecipeComments_UserId" ON "RecipeComments" ("UserId");
