INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
    (gen_random_uuid(), 'Client', 'CLIENT', gen_random_uuid()::text),
    (gen_random_uuid(), 'Admin', 'ADMIN', gen_random_uuid()::text)
ON CONFLICT ("NormalizedName") DO NOTHING;