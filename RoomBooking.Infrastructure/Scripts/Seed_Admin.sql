DO $$
DECLARE
    admin_id uuid := gen_random_uuid();
    admin_role_id uuid;
    client_role_id uuid;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "AspNetUsers" WHERE "NormalizedEmail" = 'LUBOMIRPETROV307@GMAIL.COM'
    ) THEN

        INSERT INTO "AspNetUsers" (
            "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
            "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
            "FirstName", "SecondName", "LastName", "PhoneNumber", "CreatedAt"
        )
        VALUES (
            admin_id,
            'lubomirpetrov307@gmail.com',
            'LUBOMIRPETROV307@GMAIL.COM',
            'lubomirpetrov307@gmail.com',
            'LUBOMIRPETROV307@GMAIL.COM',
            true,
            'AQAAAAIAAYagAAAAEC9T+Td+Cx5HSVvlRFx+34UCYn9biEdIcus6T77Hg+jqZzOjfaPrsrItmqcXLbzA7Q==',
            gen_random_uuid()::text,
            gen_random_uuid()::text,
            false,
            false,
            true,
            0,
            'Liubomyr',
            'Petrov',
            'Vadimovich',
            '+380000000000',
            now()
        );

        SELECT "Id" INTO admin_role_id FROM "AspNetRoles" WHERE "NormalizedName" = 'ADMIN';
        SELECT "Id" INTO client_role_id FROM "AspNetRoles" WHERE "NormalizedName" = 'CLIENT';

        INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
        VALUES
            (admin_id, admin_role_id),
            (admin_id, client_role_id);

    END IF;
END $$;