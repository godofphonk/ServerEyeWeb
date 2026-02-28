CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "MonitoredServers" (
    "Id" uuid NOT NULL,
    "ServerId" text NOT NULL,
    "ServerKey" text NOT NULL,
    "Hostname" text NOT NULL,
    "OperatingSystem" text NOT NULL,
    "AgentVersion" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastSeen" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_MonitoredServers" PRIMARY KEY ("Id")
);

CREATE TABLE "Servers" (
    "Id" uuid NOT NULL,
    "ServerKey" text NOT NULL,
    "ServerId" text NOT NULL,
    "ServerName" text NOT NULL,
    "UserId" uuid NOT NULL,
    CONSTRAINT "PK_Servers" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "UserName" text NOT NULL,
    "Password" text NOT NULL,
    "Email" text NOT NULL,
    "ServerId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserServerAccesses" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ServerId" uuid NOT NULL,
    "AccessLevel" integer NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserServerAccesses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserServerAccesses_MonitoredServers_ServerId" FOREIGN KEY ("ServerId") REFERENCES "MonitoredServers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserServerAccesses_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_MonitoredServers_ServerId" ON "MonitoredServers" ("ServerId");

CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

CREATE UNIQUE INDEX "IX_Servers_ServerKey" ON "Servers" ("ServerKey");

CREATE INDEX "IX_UserServerAccesses_ServerId" ON "UserServerAccesses" ("ServerId");

CREATE UNIQUE INDEX "IX_UserServerAccesses_UserId_ServerId" ON "UserServerAccesses" ("UserId", "ServerId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260215162850_AddServerMetricsEntities', '10.0.2');

COMMIT;

START TRANSACTION;
ALTER TABLE "Users" ADD "Role" integer NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260224183739_AddUserRoleToUser', '10.0.2');

COMMIT;

START TRANSACTION;
ALTER TABLE "Users" ADD "EmailVerifiedAt" timestamp with time zone;

ALTER TABLE "Users" ADD "IsEmailVerified" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Users" ADD "PendingEmail" text;

CREATE TABLE "AccountDeletions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Email" text NOT NULL,
    "ConfirmationCode" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsUsed" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AccountDeletions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountDeletions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "EmailVerifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Email" text NOT NULL,
    "Code" text NOT NULL,
    "Type" integer NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsUsed" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "AttemptCount" integer NOT NULL,
    CONSTRAINT "PK_EmailVerifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmailVerifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PasswordResetTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsUsed" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IpAddress" text,
    CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PasswordResetTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AccountDeletions_UserId_ConfirmationCode_IsUsed" ON "AccountDeletions" ("UserId", "ConfirmationCode", "IsUsed");

CREATE INDEX "IX_EmailVerifications_UserId_Code_IsUsed" ON "EmailVerifications" ("UserId", "Code", "IsUsed");

CREATE INDEX "IX_PasswordResetTokens_UserId_Token_IsUsed" ON "PasswordResetTokens" ("UserId", "Token", "IsUsed");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260226184910_AddEmailVerificationAndAccountDeletion', '10.0.2');

COMMIT;

START TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260226192302_AddEmailFeatures', '10.0.2');

COMMIT;

