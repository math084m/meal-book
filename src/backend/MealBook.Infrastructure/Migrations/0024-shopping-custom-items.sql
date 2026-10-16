CREATE TABLE "ShoppingCustomItems" (
    "Id" uuid NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Quantity" numeric(14, 3) NOT NULL DEFAULT 0,
    "Unit" character varying(40) NOT NULL DEFAULT '',
    "SectionSlug" character varying(20) NOT NULL,
    "IsChecked" boolean NOT NULL DEFAULT false,
    "AddedByUserId" character varying(450) NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ShoppingCustomItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ShoppingCustomItems_ShopSections" FOREIGN KEY ("SectionSlug")
        REFERENCES "ShopSections" ("Slug"),
    CONSTRAINT "CK_ShoppingCustomItems_Quantity" CHECK ("Quantity" >= 0)
);
