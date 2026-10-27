CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "AspNetRoles" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT COLLATE BINARY NOT NULL,
    "NormalizedName" TEXT COLLATE BINARY NOT NULL,
    "ConcurrencyStamp" TEXT COLLATE BINARY NOT NULL,
    CONSTRAINT "CK_AspNetRoles_ConcurrencyStampLength" CHECK ("ConcurrencyStamp" IS NULL OR length("ConcurrencyStamp") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetRoles_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetRoles_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_AspNetRoles_NameLength" CHECK ("Name" IS NULL OR length("Name") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetRoles_NormalizedNameLength" CHECK ("NormalizedName" IS NULL OR length("NormalizedName") BETWEEN 1 AND 256)
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "UserName" TEXT COLLATE BINARY NOT NULL,
    "NormalizedUserName" TEXT COLLATE BINARY NOT NULL,
    "Email" TEXT COLLATE BINARY NOT NULL,
    "NormalizedEmail" TEXT COLLATE BINARY NOT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT COLLATE BINARY NOT NULL,
    "SecurityStamp" TEXT COLLATE BINARY NOT NULL,
    "ConcurrencyStamp" TEXT COLLATE BINARY NOT NULL,
    "PhoneNumber" TEXT COLLATE BINARY NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" INTEGER NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL,
    CONSTRAINT "CK_AspNetUsers_ConcurrencyStampLength" CHECK ("ConcurrencyStamp" IS NULL OR length("ConcurrencyStamp") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUsers_EmailConfirmedBool" CHECK ("EmailConfirmed" IS NULL OR "EmailConfirmed" IN (0,1)),
    CONSTRAINT "CK_AspNetUsers_EmailLength" CHECK ("Email" IS NULL OR length("Email") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUsers_FailedCount" CHECK (AccessFailedCount>=0),
    CONSTRAINT "CK_AspNetUsers_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetUsers_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_AspNetUsers_LockoutEnabledBool" CHECK ("LockoutEnabled" IS NULL OR "LockoutEnabled" IN (0,1)),
    CONSTRAINT "CK_AspNetUsers_LockoutEndRange" CHECK ("LockoutEnd" IS NULL OR "LockoutEnd" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_AspNetUsers_NormalizedEmailLength" CHECK ("NormalizedEmail" IS NULL OR length("NormalizedEmail") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUsers_NormalizedUserNameLength" CHECK ("NormalizedUserName" IS NULL OR length("NormalizedUserName") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUsers_PasswordHashLength" CHECK ("PasswordHash" IS NULL OR length("PasswordHash") BETWEEN 1 AND 2048),
    CONSTRAINT "CK_AspNetUsers_PhoneNumberConfirmedBool" CHECK ("PhoneNumberConfirmed" IS NULL OR "PhoneNumberConfirmed" IN (0,1)),
    CONSTRAINT "CK_AspNetUsers_PhoneNumberLength" CHECK ("PhoneNumber" IS NULL OR length("PhoneNumber") BETWEEN 1 AND 64),
    CONSTRAINT "CK_AspNetUsers_SecurityStampLength" CHECK ("SecurityStamp" IS NULL OR length("SecurityStamp") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUsers_TwoFactorEnabledBool" CHECK ("TwoFactorEnabled" IS NULL OR "TwoFactorEnabled" IN (0,1)),
    CONSTRAINT "CK_AspNetUsers_UserNameLength" CHECK ("UserName" IS NULL OR length("UserName") BETWEEN 1 AND 256)
);

CREATE TABLE "SourceState" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_SourceState" PRIMARY KEY,
    "LogicalName" TEXT COLLATE BINARY NOT NULL,
    "ConfigurationVersion" INTEGER NOT NULL,
    "Kind" TEXT COLLATE BINARY NOT NULL,
    "Category" TEXT COLLATE BINARY NOT NULL,
    "Provenance" TEXT COLLATE BINARY NOT NULL,
    "DisplayName" TEXT COLLATE BINARY NOT NULL,
    "ConfigurationReference" TEXT COLLATE BINARY NOT NULL,
    "IdentityVersion" INTEGER NOT NULL,
    "ScheduleVersion" INTEGER NULL,
    "PollIntervalMs" INTEGER NULL,
    "Enabled" INTEGER NOT NULL,
    "ActivatedAtUtcMs" INTEGER NULL,
    "Health" TEXT COLLATE BINARY NOT NULL,
    "BaselineCompletedAtUtcMs" INTEGER NULL,
    "ETag" TEXT COLLATE BINARY NULL,
    "LastModified" TEXT COLLATE BINARY NULL,
    "LastAttemptAtUtcMs" INTEGER NULL,
    "LastFetchSucceededAtUtcMs" INTEGER NULL,
    "LastValidObservationAtUtcMs" INTEGER NULL,
    "NextPollAtUtcMs" INTEGER NULL,
    "ConsecutiveFailures" INTEGER NOT NULL,
    "ConsecutiveSchemaFailures" INTEGER NOT NULL,
    "LastErrorCode" TEXT COLLATE BINARY NULL,
    "LastErrorSummary" TEXT COLLATE BINARY NULL,
    "LastPollRejectedItems" INTEGER NOT NULL,
    "LastPollConflictingItems" INTEGER NOT NULL,
    "Version" INTEGER NOT NULL,
    "CreatedAtUtcMs" INTEGER NOT NULL,
    "UpdatedAtUtcMs" INTEGER NOT NULL,
    CONSTRAINT "AK_SourceState_Id_Category_Provenance" UNIQUE ("Id", "Category", "Provenance"),
    CONSTRAINT "CK_SourceState_ActivatedAtUtcMsRange" CHECK ("ActivatedAtUtcMs" IS NULL OR "ActivatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_Activation" CHECK (Enabled=0 OR ActivatedAtUtcMs IS NOT NULL),
    CONSTRAINT "CK_SourceState_Baseline" CHECK (Kind='Rss' OR (BaselineCompletedAtUtcMs IS NULL AND ETag IS NULL AND LastModified IS NULL)),
    CONSTRAINT "CK_SourceState_BaselineCompletedAtUtcMsRange" CHECK ("BaselineCompletedAtUtcMs" IS NULL OR "BaselineCompletedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_CategoryLength" CHECK ("Category" IS NULL OR length("Category") BETWEEN 1 AND 10),
    CONSTRAINT "CK_SourceState_ConfigurationReferenceLength" CHECK ("ConfigurationReference" IS NULL OR length("ConfigurationReference") BETWEEN 1 AND 128),
    CONSTRAINT "CK_SourceState_ConfigurationVersionPositive" CHECK ("ConfigurationVersion" IS NULL OR "ConfigurationVersion">=1),
    CONSTRAINT "CK_SourceState_ConsecutiveFailuresCounter" CHECK ("ConsecutiveFailures">=0),
    CONSTRAINT "CK_SourceState_ConsecutiveSchemaFailuresCounter" CHECK ("ConsecutiveSchemaFailures">=0),
    CONSTRAINT "CK_SourceState_CreatedAtUtcMsRange" CHECK ("CreatedAtUtcMs" IS NULL OR "CreatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_DisplayNameLength" CHECK ("DisplayName" IS NULL OR length("DisplayName") BETWEEN 1 AND 100),
    CONSTRAINT "CK_SourceState_EnabledBool" CHECK ("Enabled" IS NULL OR "Enabled" IN (0,1)),
    CONSTRAINT "CK_SourceState_ETagLength" CHECK ("ETag" IS NULL OR length("ETag") BETWEEN 1 AND 1024),
    CONSTRAINT "CK_SourceState_Health" CHECK (Health IN ('Uninitialized','Healthy','Degraded','Blocked','Disabled')),
    CONSTRAINT "CK_SourceState_HealthLength" CHECK ("Health" IS NULL OR length("Health") BETWEEN 1 AND 16),
    CONSTRAINT "CK_SourceState_IdentityVersionPositive" CHECK ("IdentityVersion" IS NULL OR "IdentityVersion">=1),
    CONSTRAINT "CK_SourceState_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_SourceState_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_SourceState_Kind" CHECK ((Kind='Rss' AND Category='News' AND Provenance='Live') OR (Kind='Market' AND Category='Market' AND Provenance='Live') OR (Kind='EarthquakeWebhook' AND Category='Earthquake' AND Provenance='Simulated') OR (Kind='Manual' AND Category IN ('News','Market','Earthquake') AND Provenance='Manual')),
    CONSTRAINT "CK_SourceState_KindLength" CHECK ("Kind" IS NULL OR length("Kind") BETWEEN 1 AND 20),
    CONSTRAINT "CK_SourceState_LastAttemptAtUtcMsRange" CHECK ("LastAttemptAtUtcMs" IS NULL OR "LastAttemptAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_LastErrorCodeLength" CHECK ("LastErrorCode" IS NULL OR length("LastErrorCode") BETWEEN 1 AND 64),
    CONSTRAINT "CK_SourceState_LastErrorSummaryLength" CHECK ("LastErrorSummary" IS NULL OR length("LastErrorSummary") BETWEEN 1 AND 500),
    CONSTRAINT "CK_SourceState_LastFetchSucceededAtUtcMsRange" CHECK ("LastFetchSucceededAtUtcMs" IS NULL OR "LastFetchSucceededAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_LastModifiedLength" CHECK ("LastModified" IS NULL OR length("LastModified") BETWEEN 1 AND 128),
    CONSTRAINT "CK_SourceState_LastPollConflictingItemsCounter" CHECK ("LastPollConflictingItems">=0),
    CONSTRAINT "CK_SourceState_LastPollRejectedItemsCounter" CHECK ("LastPollRejectedItems">=0),
    CONSTRAINT "CK_SourceState_LastValidObservationAtUtcMsRange" CHECK ("LastValidObservationAtUtcMs" IS NULL OR "LastValidObservationAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_LogicalNameLength" CHECK ("LogicalName" IS NULL OR length("LogicalName") BETWEEN 1 AND 100),
    CONSTRAINT "CK_SourceState_NextPollAtUtcMsRange" CHECK ("NextPollAtUtcMs" IS NULL OR "NextPollAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_ProvenanceLength" CHECK ("Provenance" IS NULL OR length("Provenance") BETWEEN 1 AND 12),
    CONSTRAINT "CK_SourceState_Schedule" CHECK ((Kind IN ('Rss','Market') AND ScheduleVersion IS NOT NULL AND PollIntervalMs IS NOT NULL AND PollIntervalMs>0) OR (Kind NOT IN ('Rss','Market') AND ScheduleVersion IS NULL AND PollIntervalMs IS NULL AND NextPollAtUtcMs IS NULL)),
    CONSTRAINT "CK_SourceState_ScheduleVersionPositive" CHECK ("ScheduleVersion" IS NULL OR "ScheduleVersion">=1),
    CONSTRAINT "CK_SourceState_UpdatedAtUtcMsRange" CHECK ("UpdatedAtUtcMs" IS NULL OR "UpdatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceState_VersionPositive" CHECK ("Version" IS NULL OR "Version">=1)
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT COLLATE BINARY NOT NULL,
    "ClaimType" TEXT COLLATE BINARY NULL,
    "ClaimValue" TEXT COLLATE BINARY NULL,
    CONSTRAINT "CK_AspNetRoleClaims_ClaimTypeLength" CHECK ("ClaimType" IS NULL OR length("ClaimType") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetRoleClaims_ClaimValueLength" CHECK ("ClaimValue" IS NULL OR length("ClaimValue") BETWEEN 1 AND 2048),
    CONSTRAINT "CK_AspNetRoleClaims_RoleIdGuid" CHECK ("RoleId" IS NULL OR (length("RoleId")=36 AND substr("RoleId",9,1)='-' AND substr("RoleId",14,1)='-' AND substr("RoleId",19,1)='-' AND substr("RoleId",24,1)='-' AND length(replace("RoleId",'-',''))=32 AND replace("RoleId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetRoleClaims_RoleIdLength" CHECK ("RoleId" IS NULL OR length("RoleId") BETWEEN 1 AND 36),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT COLLATE BINARY NOT NULL,
    "ClaimType" TEXT COLLATE BINARY NULL,
    "ClaimValue" TEXT COLLATE BINARY NULL,
    CONSTRAINT "CK_AspNetUserClaims_ClaimTypeLength" CHECK ("ClaimType" IS NULL OR length("ClaimType") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUserClaims_ClaimValueLength" CHECK ("ClaimValue" IS NULL OR length("ClaimValue") BETWEEN 1 AND 2048),
    CONSTRAINT "CK_AspNetUserClaims_UserIdGuid" CHECK ("UserId" IS NULL OR (length("UserId")=36 AND substr("UserId",9,1)='-' AND substr("UserId",14,1)='-' AND substr("UserId",19,1)='-' AND substr("UserId",24,1)='-' AND length(replace("UserId",'-',''))=32 AND replace("UserId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetUserClaims_UserIdLength" CHECK ("UserId" IS NULL OR length("UserId") BETWEEN 1 AND 36),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT COLLATE BINARY NOT NULL,
    "ProviderKey" TEXT COLLATE BINARY NOT NULL,
    "ProviderDisplayName" TEXT COLLATE BINARY NULL,
    "UserId" TEXT COLLATE BINARY NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "CK_AspNetUserLogins_LoginProviderLength" CHECK ("LoginProvider" IS NULL OR length("LoginProvider") BETWEEN 1 AND 128),
    CONSTRAINT "CK_AspNetUserLogins_ProviderDisplayNameLength" CHECK ("ProviderDisplayName" IS NULL OR length("ProviderDisplayName") BETWEEN 1 AND 256),
    CONSTRAINT "CK_AspNetUserLogins_ProviderKeyLength" CHECK ("ProviderKey" IS NULL OR length("ProviderKey") BETWEEN 1 AND 128),
    CONSTRAINT "CK_AspNetUserLogins_UserIdGuid" CHECK ("UserId" IS NULL OR (length("UserId")=36 AND substr("UserId",9,1)='-' AND substr("UserId",14,1)='-' AND substr("UserId",19,1)='-' AND substr("UserId",24,1)='-' AND length(replace("UserId",'-',''))=32 AND replace("UserId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetUserLogins_UserIdLength" CHECK ("UserId" IS NULL OR length("UserId") BETWEEN 1 AND 36),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT COLLATE BINARY NOT NULL,
    "RoleId" TEXT COLLATE BINARY NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "CK_AspNetUserRoles_RoleIdGuid" CHECK ("RoleId" IS NULL OR (length("RoleId")=36 AND substr("RoleId",9,1)='-' AND substr("RoleId",14,1)='-' AND substr("RoleId",19,1)='-' AND substr("RoleId",24,1)='-' AND length(replace("RoleId",'-',''))=32 AND replace("RoleId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetUserRoles_RoleIdLength" CHECK ("RoleId" IS NULL OR length("RoleId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_AspNetUserRoles_UserIdGuid" CHECK ("UserId" IS NULL OR (length("UserId")=36 AND substr("UserId",9,1)='-' AND substr("UserId",14,1)='-' AND substr("UserId",19,1)='-' AND substr("UserId",24,1)='-' AND length(replace("UserId",'-',''))=32 AND replace("UserId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetUserRoles_UserIdLength" CHECK ("UserId" IS NULL OR length("UserId") BETWEEN 1 AND 36),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT COLLATE BINARY NOT NULL,
    "LoginProvider" TEXT COLLATE BINARY NOT NULL,
    "Name" TEXT COLLATE BINARY NOT NULL,
    "Value" TEXT COLLATE BINARY NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "CK_AspNetUserTokens_LoginProviderLength" CHECK ("LoginProvider" IS NULL OR length("LoginProvider") BETWEEN 1 AND 128),
    CONSTRAINT "CK_AspNetUserTokens_NameLength" CHECK ("Name" IS NULL OR length("Name") BETWEEN 1 AND 128),
    CONSTRAINT "CK_AspNetUserTokens_UserIdGuid" CHECK ("UserId" IS NULL OR (length("UserId")=36 AND substr("UserId",9,1)='-' AND substr("UserId",14,1)='-' AND substr("UserId",19,1)='-' AND substr("UserId",24,1)='-' AND length(replace("UserId",'-',''))=32 AND replace("UserId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_AspNetUserTokens_UserIdLength" CHECK ("UserId" IS NULL OR length("UserId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_AspNetUserTokens_ValueLength" CHECK ("Value" IS NULL OR length("Value") BETWEEN 1 AND 4096),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ChannelDestination" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_ChannelDestination" PRIMARY KEY,
    "OwnerId" TEXT COLLATE BINARY NOT NULL,
    "Channel" TEXT COLLATE BINARY NOT NULL,
    "ConfigurationVersion" INTEGER NOT NULL,
    "DestinationIdentity" TEXT COLLATE BINARY NOT NULL,
    "Address" TEXT COLLATE BINARY NULL,
    "SecretReference" TEXT COLLATE BINARY NULL,
    "IsAvailable" INTEGER NOT NULL,
    "RevokedAtUtcMs" INTEGER NULL,
    "CreatedAtUtcMs" INTEGER NOT NULL,
    CONSTRAINT "AK_ChannelDestination_Id_OwnerId_Channel" UNIQUE ("Id", "OwnerId", "Channel"),
    CONSTRAINT "CK_ChannelDestination_Address" CHECK ((Channel='Email' AND Address IS NOT NULL AND SecretReference IS NULL) OR (Channel='Slack' AND SecretReference IS NOT NULL AND Address IS NULL)),
    CONSTRAINT "CK_ChannelDestination_AddressLength" CHECK ("Address" IS NULL OR length("Address") BETWEEN 1 AND 320),
    CONSTRAINT "CK_ChannelDestination_Availability" CHECK (IsAvailable=0 OR RevokedAtUtcMs IS NULL),
    CONSTRAINT "CK_ChannelDestination_Channel" CHECK (Channel IN ('Email','Slack')),
    CONSTRAINT "CK_ChannelDestination_ChannelLength" CHECK ("Channel" IS NULL OR length("Channel") BETWEEN 1 AND 5),
    CONSTRAINT "CK_ChannelDestination_ConfigurationVersionPositive" CHECK ("ConfigurationVersion" IS NULL OR "ConfigurationVersion">=1),
    CONSTRAINT "CK_ChannelDestination_CreatedAtUtcMsRange" CHECK ("CreatedAtUtcMs" IS NULL OR "CreatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_ChannelDestination_DestinationIdentityLength" CHECK ("DestinationIdentity" IS NULL OR length("DestinationIdentity") BETWEEN 1 AND 128),
    CONSTRAINT "CK_ChannelDestination_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_ChannelDestination_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_ChannelDestination_IsAvailableBool" CHECK ("IsAvailable" IS NULL OR "IsAvailable" IN (0,1)),
    CONSTRAINT "CK_ChannelDestination_OwnerIdGuid" CHECK ("OwnerId" IS NULL OR (length("OwnerId")=36 AND substr("OwnerId",9,1)='-' AND substr("OwnerId",14,1)='-' AND substr("OwnerId",19,1)='-' AND substr("OwnerId",24,1)='-' AND length(replace("OwnerId",'-',''))=32 AND replace("OwnerId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_ChannelDestination_OwnerIdLength" CHECK ("OwnerId" IS NULL OR length("OwnerId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_ChannelDestination_RevokedAtUtcMsRange" CHECK ("RevokedAtUtcMs" IS NULL OR "RevokedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_ChannelDestination_SecretReferenceLength" CHECK ("SecretReference" IS NULL OR length("SecretReference") BETWEEN 1 AND 256),
    CONSTRAINT "FK_ChannelDestination_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Rule" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_Rule" PRIMARY KEY,
    "OwnerId" TEXT COLLATE BINARY NOT NULL,
    "Name" TEXT COLLATE BINARY NOT NULL,
    "Category" TEXT COLLATE BINARY NOT NULL,
    "Keyword" TEXT COLLATE BINARY NULL,
    "Instrument" TEXT COLLATE BINARY NULL,
    "ComparisonKind" TEXT COLLATE BINARY NULL,
    "ThresholdPercentText" TEXT COLLATE BINARY NULL,
    "MinimumMagnitudeText" TEXT COLLATE BINARY NULL,
    "MagnitudeScale" TEXT COLLATE BINARY NULL,
    "EmailSelected" INTEGER NOT NULL,
    "SlackSelected" INTEGER NOT NULL,
    "Enabled" INTEGER NOT NULL,
    "Version" INTEGER NOT NULL,
    "CreatedAtUtcMs" INTEGER NOT NULL,
    "UpdatedAtUtcMs" INTEGER NOT NULL,
    "DeletedAtUtcMs" INTEGER NULL,
    CONSTRAINT "AK_Rule_Id_OwnerId" UNIQUE ("Id", "OwnerId"),
    CONSTRAINT "CK_Rule_CategoryLength" CHECK ("Category" IS NULL OR length("Category") BETWEEN 1 AND 10),
    CONSTRAINT "CK_Rule_ComparisonKindLength" CHECK ("ComparisonKind" IS NULL OR length("ComparisonKind") BETWEEN 1 AND 32),
    CONSTRAINT "CK_Rule_CreatedAtUtcMsRange" CHECK ("CreatedAtUtcMs" IS NULL OR "CreatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Rule_DeletedAtUtcMsRange" CHECK ("DeletedAtUtcMs" IS NULL OR "DeletedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Rule_EmailSelectedBool" CHECK ("EmailSelected" IS NULL OR "EmailSelected" IN (0,1)),
    CONSTRAINT "CK_Rule_EnabledBool" CHECK ("Enabled" IS NULL OR "Enabled" IN (0,1)),
    CONSTRAINT "CK_Rule_Filter" CHECK ((Category='News' AND Keyword IS NOT NULL AND Instrument IS NULL AND ComparisonKind IS NULL AND ThresholdPercentText IS NULL AND MinimumMagnitudeText IS NULL AND MagnitudeScale IS NULL) OR (Category='Market' AND Keyword IS NULL AND Instrument IS NOT NULL AND Instrument='BTCUSDT' AND ComparisonKind IS NOT NULL AND ComparisonKind='binance-rolling-24h' AND ThresholdPercentText IS NOT NULL AND MinimumMagnitudeText IS NULL AND MagnitudeScale IS NULL) OR (Category='Earthquake' AND Keyword IS NULL AND Instrument IS NULL AND ComparisonKind IS NULL AND ThresholdPercentText IS NULL AND MinimumMagnitudeText IS NOT NULL AND MagnitudeScale IS NOT NULL AND MagnitudeScale='Mw')),
    CONSTRAINT "CK_Rule_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_Rule_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_Rule_InstrumentLength" CHECK ("Instrument" IS NULL OR length("Instrument") BETWEEN 1 AND 20),
    CONSTRAINT "CK_Rule_KeywordLength" CHECK ("Keyword" IS NULL OR length("Keyword") BETWEEN 1 AND 100),
    CONSTRAINT "CK_Rule_MagnitudeScaleLength" CHECK ("MagnitudeScale" IS NULL OR length("MagnitudeScale") BETWEEN 1 AND 4),
    CONSTRAINT "CK_Rule_MinimumMagnitudeTextLength" CHECK ("MinimumMagnitudeText" IS NULL OR length("MinimumMagnitudeText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Rule_NameLength" CHECK ("Name" IS NULL OR length("Name") BETWEEN 1 AND 100),
    CONSTRAINT "CK_Rule_OwnerIdGuid" CHECK ("OwnerId" IS NULL OR (length("OwnerId")=36 AND substr("OwnerId",9,1)='-' AND substr("OwnerId",14,1)='-' AND substr("OwnerId",19,1)='-' AND substr("OwnerId",24,1)='-' AND length(replace("OwnerId",'-',''))=32 AND replace("OwnerId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_Rule_OwnerIdLength" CHECK ("OwnerId" IS NULL OR length("OwnerId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_Rule_Selection" CHECK ((EmailSelected=1 OR SlackSelected=1) AND (DeletedAtUtcMs IS NULL OR Enabled=0)),
    CONSTRAINT "CK_Rule_SlackSelectedBool" CHECK ("SlackSelected" IS NULL OR "SlackSelected" IN (0,1)),
    CONSTRAINT "CK_Rule_ThresholdPercentTextLength" CHECK ("ThresholdPercentText" IS NULL OR length("ThresholdPercentText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Rule_UpdatedAtUtcMsRange" CHECK ("UpdatedAtUtcMs" IS NULL OR "UpdatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Rule_VersionPositive" CHECK ("Version" IS NULL OR "Version">=1),
    CONSTRAINT "FK_Rule_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Event" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_Event" PRIMARY KEY,
    "SourceId" TEXT COLLATE BINARY NOT NULL,
    "ExternalKey" TEXT COLLATE BINARY NOT NULL,
    "Category" TEXT COLLATE BINARY NOT NULL,
    "Provenance" TEXT COLLATE BINARY NOT NULL,
    "SubmittedByUserId" TEXT COLLATE BINARY NULL,
    "SchemaVersion" INTEGER NOT NULL,
    "FingerprintVersion" INTEGER NOT NULL,
    "PayloadFingerprint" BLOB NOT NULL,
    "OccurredAtUtcMs" INTEGER NOT NULL,
    "ReceivedAtUtcMs" INTEGER NOT NULL,
    "AcceptedAtUtcMs" INTEGER NOT NULL,
    "PredicateVersion" INTEGER NOT NULL,
    "EvaluatedRuleCount" INTEGER NOT NULL,
    "MatchedRuleCount" INTEGER NOT NULL,
    "IntentCount" INTEGER NOT NULL,
    "Title" TEXT COLLATE BINARY NULL,
    "Summary" TEXT COLLATE BINARY NULL,
    "ArticleUrl" TEXT COLLATE BINARY NULL,
    "SummaryTruncated" INTEGER NULL,
    "Instrument" TEXT COLLATE BINARY NULL,
    "QuoteCurrency" TEXT COLLATE BINARY NULL,
    "ComparisonKind" TEXT COLLATE BINARY NULL,
    "WindowStartUtcMs" INTEGER NULL,
    "WindowEndUtcMs" INTEGER NULL,
    "ScheduledSlotUtcMs" INTEGER NULL,
    "OpenPriceText" TEXT COLLATE BINARY NULL,
    "LastPriceText" TEXT COLLATE BINARY NULL,
    "SignedMovementPercentText" TEXT COLLATE BINARY NULL,
    "ProviderMovementPercentText" TEXT COLLATE BINARY NULL,
    "MagnitudeText" TEXT COLLATE BINARY NULL,
    "MagnitudeScale" TEXT COLLATE BINARY NULL,
    "Place" TEXT COLLATE BINARY NULL,
    "Simulated" INTEGER NULL,
    CONSTRAINT "AK_Event_Id_SourceId_ExternalKey" UNIQUE ("Id", "SourceId", "ExternalKey"),
    CONSTRAINT "CK_Event_AcceptedAtUtcMsRange" CHECK ("AcceptedAtUtcMs" IS NULL OR "AcceptedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Event_ArticleUrlLength" CHECK ("ArticleUrl" IS NULL OR length("ArticleUrl") BETWEEN 1 AND 2048),
    CONSTRAINT "CK_Event_CategoryFields" CHECK ((Category='News' AND Title IS NOT NULL AND SummaryTruncated IS NOT NULL AND Instrument IS NULL AND QuoteCurrency IS NULL AND ComparisonKind IS NULL AND WindowStartUtcMs IS NULL AND WindowEndUtcMs IS NULL AND ScheduledSlotUtcMs IS NULL AND OpenPriceText IS NULL AND LastPriceText IS NULL AND SignedMovementPercentText IS NULL AND ProviderMovementPercentText IS NULL AND MagnitudeText IS NULL AND MagnitudeScale IS NULL AND Place IS NULL AND Simulated IS NULL) OR (Category='Market' AND Title IS NULL AND Summary IS NULL AND ArticleUrl IS NULL AND SummaryTruncated IS NULL AND MagnitudeText IS NULL AND MagnitudeScale IS NULL AND Place IS NULL AND Simulated IS NULL AND Instrument IS NOT NULL AND Instrument='BTCUSDT' AND QuoteCurrency IS NOT NULL AND QuoteCurrency='USDT' AND ComparisonKind IS NOT NULL AND ComparisonKind='binance-rolling-24h' AND WindowStartUtcMs IS NOT NULL AND WindowEndUtcMs IS NOT NULL AND WindowEndUtcMs-WindowStartUtcMs=86400000 AND OccurredAtUtcMs=WindowEndUtcMs AND OpenPriceText IS NOT NULL AND LastPriceText IS NOT NULL AND SignedMovementPercentText IS NOT NULL AND ((Provenance='Live' AND ScheduledSlotUtcMs IS NOT NULL AND ScheduledSlotUtcMs%60000=0) OR (Provenance='Manual' AND ScheduledSlotUtcMs IS NULL))) OR (Category='Earthquake' AND Title IS NULL AND Summary IS NULL AND ArticleUrl IS NULL AND SummaryTruncated IS NULL AND Instrument IS NULL AND QuoteCurrency IS NULL AND ComparisonKind IS NULL AND WindowStartUtcMs IS NULL AND WindowEndUtcMs IS NULL AND ScheduledSlotUtcMs IS NULL AND OpenPriceText IS NULL AND LastPriceText IS NULL AND SignedMovementPercentText IS NULL AND ProviderMovementPercentText IS NULL AND MagnitudeText IS NOT NULL AND MagnitudeScale IS NOT NULL AND MagnitudeScale='Mw' AND Simulated IS NOT NULL AND Simulated=1)),
    CONSTRAINT "CK_Event_CategoryLength" CHECK ("Category" IS NULL OR length("Category") BETWEEN 1 AND 10),
    CONSTRAINT "CK_Event_ComparisonKindLength" CHECK ("ComparisonKind" IS NULL OR length("ComparisonKind") BETWEEN 1 AND 32),
    CONSTRAINT "CK_Event_Counts" CHECK (EvaluatedRuleCount BETWEEN 0 AND 100 AND MatchedRuleCount BETWEEN 0 AND EvaluatedRuleCount AND IntentCount BETWEEN MatchedRuleCount AND 2*MatchedRuleCount AND IntentCount<=200),
    CONSTRAINT "CK_Event_ExternalKeyLength" CHECK ("ExternalKey" IS NULL OR length("ExternalKey") BETWEEN 1 AND 12288),
    CONSTRAINT "CK_Event_FingerprintVersionPositive" CHECK ("FingerprintVersion" IS NULL OR "FingerprintVersion">=1),
    CONSTRAINT "CK_Event_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_Event_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_Event_InstrumentLength" CHECK ("Instrument" IS NULL OR length("Instrument") BETWEEN 1 AND 20),
    CONSTRAINT "CK_Event_LastPriceTextLength" CHECK ("LastPriceText" IS NULL OR length("LastPriceText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Event_MagnitudeScaleLength" CHECK ("MagnitudeScale" IS NULL OR length("MagnitudeScale") BETWEEN 1 AND 4),
    CONSTRAINT "CK_Event_MagnitudeTextLength" CHECK ("MagnitudeText" IS NULL OR length("MagnitudeText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Event_Manual" CHECK ((Provenance='Manual' AND SubmittedByUserId IS NOT NULL) OR (Provenance IN ('Live','Simulated') AND SubmittedByUserId IS NULL)),
    CONSTRAINT "CK_Event_OccurredAtUtcMsRange" CHECK ("OccurredAtUtcMs" IS NULL OR "OccurredAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Event_OpenPriceTextLength" CHECK ("OpenPriceText" IS NULL OR length("OpenPriceText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Event_PayloadFingerprintHash" CHECK ("PayloadFingerprint" IS NULL OR length("PayloadFingerprint")=32),
    CONSTRAINT "CK_Event_PlaceLength" CHECK ("Place" IS NULL OR length("Place") BETWEEN 1 AND 200),
    CONSTRAINT "CK_Event_PredicateVersionPositive" CHECK ("PredicateVersion" IS NULL OR "PredicateVersion">=1),
    CONSTRAINT "CK_Event_ProvenanceLength" CHECK ("Provenance" IS NULL OR length("Provenance") BETWEEN 1 AND 12),
    CONSTRAINT "CK_Event_ProviderMovementPercentTextLength" CHECK ("ProviderMovementPercentText" IS NULL OR length("ProviderMovementPercentText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Event_QuoteCurrencyLength" CHECK ("QuoteCurrency" IS NULL OR length("QuoteCurrency") BETWEEN 1 AND 10),
    CONSTRAINT "CK_Event_ReceivedAtUtcMsRange" CHECK ("ReceivedAtUtcMs" IS NULL OR "ReceivedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Event_ScheduledSlotUtcMsRange" CHECK ("ScheduledSlotUtcMs" IS NULL OR "ScheduledSlotUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Event_SchemaVersionPositive" CHECK ("SchemaVersion" IS NULL OR "SchemaVersion">=1),
    CONSTRAINT "CK_Event_SignedMovementPercentTextLength" CHECK ("SignedMovementPercentText" IS NULL OR length("SignedMovementPercentText") BETWEEN 1 AND 64),
    CONSTRAINT "CK_Event_SimulatedBool" CHECK ("Simulated" IS NULL OR "Simulated" IN (0,1)),
    CONSTRAINT "CK_Event_SourceIdGuid" CHECK ("SourceId" IS NULL OR (length("SourceId")=36 AND substr("SourceId",9,1)='-' AND substr("SourceId",14,1)='-' AND substr("SourceId",19,1)='-' AND substr("SourceId",24,1)='-' AND length(replace("SourceId",'-',''))=32 AND replace("SourceId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_Event_SourceIdLength" CHECK ("SourceId" IS NULL OR length("SourceId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_Event_SubmittedByUserIdGuid" CHECK ("SubmittedByUserId" IS NULL OR (length("SubmittedByUserId")=36 AND substr("SubmittedByUserId",9,1)='-' AND substr("SubmittedByUserId",14,1)='-' AND substr("SubmittedByUserId",19,1)='-' AND substr("SubmittedByUserId",24,1)='-' AND length(replace("SubmittedByUserId",'-',''))=32 AND replace("SubmittedByUserId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_Event_SubmittedByUserIdLength" CHECK ("SubmittedByUserId" IS NULL OR length("SubmittedByUserId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_Event_SummaryLength" CHECK ("Summary" IS NULL OR length("Summary") BETWEEN 1 AND 4000),
    CONSTRAINT "CK_Event_SummaryTruncatedBool" CHECK ("SummaryTruncated" IS NULL OR "SummaryTruncated" IN (0,1)),
    CONSTRAINT "CK_Event_TitleLength" CHECK ("Title" IS NULL OR length("Title") BETWEEN 1 AND 500),
    CONSTRAINT "CK_Event_WindowEndUtcMsRange" CHECK ("WindowEndUtcMs" IS NULL OR "WindowEndUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_Event_WindowStartUtcMsRange" CHECK ("WindowStartUtcMs" IS NULL OR "WindowStartUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "FK_Event_AspNetUsers_SubmittedByUserId" FOREIGN KEY ("SubmittedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Event_SourceState_SourceId_Category_Provenance" FOREIGN KEY ("SourceId", "Category", "Provenance") REFERENCES "SourceState" ("Id", "Category", "Provenance") ON DELETE RESTRICT
);

CREATE TABLE "SourceFailure" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_SourceFailure" PRIMARY KEY,
    "SourceId" TEXT COLLATE BINARY NOT NULL,
    "OccurredAtUtcMs" INTEGER NOT NULL,
    "Stage" TEXT COLLATE BINARY NOT NULL,
    "Code" TEXT COLLATE BINARY NOT NULL,
    "Summary" TEXT COLLATE BINARY NOT NULL,
    "HttpStatus" INTEGER NULL,
    "RejectedItemCount" INTEGER NOT NULL,
    CONSTRAINT "CK_SourceFailure_CodeLength" CHECK ("Code" IS NULL OR length("Code") BETWEEN 1 AND 64),
    CONSTRAINT "CK_SourceFailure_HttpStatusRange" CHECK ("HttpStatus" IS NULL OR "HttpStatus" BETWEEN 100 AND 599),
    CONSTRAINT "CK_SourceFailure_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_SourceFailure_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_SourceFailure_OccurredAtUtcMsRange" CHECK ("OccurredAtUtcMs" IS NULL OR "OccurredAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SourceFailure_RejectedItemCountCounter" CHECK ("RejectedItemCount">=0),
    CONSTRAINT "CK_SourceFailure_SourceIdGuid" CHECK ("SourceId" IS NULL OR (length("SourceId")=36 AND substr("SourceId",9,1)='-' AND substr("SourceId",14,1)='-' AND substr("SourceId",19,1)='-' AND substr("SourceId",24,1)='-' AND length(replace("SourceId",'-',''))=32 AND replace("SourceId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_SourceFailure_SourceIdLength" CHECK ("SourceId" IS NULL OR length("SourceId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_SourceFailure_Stage" CHECK (Stage IN ('Fetch','Parse','Normalize','Accept','Schedule')),
    CONSTRAINT "CK_SourceFailure_StageLength" CHECK ("Stage" IS NULL OR length("Stage") BETWEEN 1 AND 16),
    CONSTRAINT "CK_SourceFailure_SummaryLength" CHECK ("Summary" IS NULL OR length("Summary") BETWEEN 1 AND 500),
    CONSTRAINT "FK_SourceFailure_SourceState_SourceId" FOREIGN KEY ("SourceId") REFERENCES "SourceState" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "NotificationIntent" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_NotificationIntent" PRIMARY KEY,
    "EventId" TEXT COLLATE BINARY NOT NULL,
    "RuleId" TEXT COLLATE BINARY NOT NULL,
    "OwnerId" TEXT COLLATE BINARY NOT NULL,
    "RuleVersion" INTEGER NOT NULL,
    "RuleNameSnapshot" TEXT COLLATE BINARY NOT NULL,
    "Category" TEXT COLLATE BINARY NOT NULL,
    "FilterSnapshotJson" TEXT COLLATE BINARY NOT NULL,
    "PredicateVersion" INTEGER NOT NULL,
    "MatchedAtUtcMs" INTEGER NOT NULL,
    "Channel" TEXT COLLATE BINARY NOT NULL,
    "DestinationId" TEXT COLLATE BINARY NOT NULL,
    "DestinationConfigurationVersion" INTEGER NOT NULL,
    "DestinationIdentitySnapshot" TEXT COLLATE BINARY NOT NULL,
    "RecipientAddressSnapshot" TEXT COLLATE BINARY NULL,
    "SecretReferenceSnapshot" TEXT COLLATE BINARY NULL,
    "SubjectSnapshot" TEXT COLLATE BINARY NULL,
    "BodySnapshot" TEXT COLLATE BINARY NOT NULL,
    "PayloadSchemaVersion" INTEGER NOT NULL,
    "CreatedAtUtcMs" INTEGER NOT NULL,
    "Status" TEXT COLLATE BINARY NOT NULL,
    "AttemptCount" INTEGER NOT NULL,
    "NextAttemptAtUtcMs" INTEGER NULL,
    "ClaimToken" TEXT COLLATE BINARY NULL,
    "LeaseExpiresAtUtcMs" INTEGER NULL,
    "DeliveredAtUtcMs" INTEGER NULL,
    "FailedAtUtcMs" INTEGER NULL,
    "LastErrorCode" TEXT COLLATE BINARY NULL,
    "LastErrorSummary" TEXT COLLATE BINARY NULL,
    "Version" INTEGER NOT NULL,
    CONSTRAINT "CK_NotificationIntent_BodySnapshotLength" CHECK ("BodySnapshot" IS NULL OR length("BodySnapshot") BETWEEN 1 AND 16000),
    CONSTRAINT "CK_NotificationIntent_Category" CHECK (Category IN ('News','Market','Earthquake')),
    CONSTRAINT "CK_NotificationIntent_CategoryLength" CHECK ("Category" IS NULL OR length("Category") BETWEEN 1 AND 10),
    CONSTRAINT "CK_NotificationIntent_Channel" CHECK ((Channel='Email' AND RecipientAddressSnapshot IS NOT NULL AND SubjectSnapshot IS NOT NULL AND SecretReferenceSnapshot IS NULL) OR (Channel='Slack' AND SecretReferenceSnapshot IS NOT NULL AND RecipientAddressSnapshot IS NULL AND SubjectSnapshot IS NULL)),
    CONSTRAINT "CK_NotificationIntent_ChannelLength" CHECK ("Channel" IS NULL OR length("Channel") BETWEEN 1 AND 5),
    CONSTRAINT "CK_NotificationIntent_ClaimTokenGuid" CHECK ("ClaimToken" IS NULL OR (length("ClaimToken")=36 AND substr("ClaimToken",9,1)='-' AND substr("ClaimToken",14,1)='-' AND substr("ClaimToken",19,1)='-' AND substr("ClaimToken",24,1)='-' AND length(replace("ClaimToken",'-',''))=32 AND replace("ClaimToken",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_NotificationIntent_ClaimTokenLength" CHECK ("ClaimToken" IS NULL OR length("ClaimToken") BETWEEN 1 AND 36),
    CONSTRAINT "CK_NotificationIntent_CreatedAtUtcMsRange" CHECK ("CreatedAtUtcMs" IS NULL OR "CreatedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_NotificationIntent_DeliveredAtUtcMsRange" CHECK ("DeliveredAtUtcMs" IS NULL OR "DeliveredAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_NotificationIntent_DestinationConfigurationVersionPositive" CHECK ("DestinationConfigurationVersion" IS NULL OR "DestinationConfigurationVersion">=1),
    CONSTRAINT "CK_NotificationIntent_DestinationIdentitySnapshotLength" CHECK ("DestinationIdentitySnapshot" IS NULL OR length("DestinationIdentitySnapshot") BETWEEN 1 AND 128),
    CONSTRAINT "CK_NotificationIntent_DestinationIdGuid" CHECK ("DestinationId" IS NULL OR (length("DestinationId")=36 AND substr("DestinationId",9,1)='-' AND substr("DestinationId",14,1)='-' AND substr("DestinationId",19,1)='-' AND substr("DestinationId",24,1)='-' AND length(replace("DestinationId",'-',''))=32 AND replace("DestinationId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_NotificationIntent_DestinationIdLength" CHECK ("DestinationId" IS NULL OR length("DestinationId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_NotificationIntent_EventIdGuid" CHECK ("EventId" IS NULL OR (length("EventId")=36 AND substr("EventId",9,1)='-' AND substr("EventId",14,1)='-' AND substr("EventId",19,1)='-' AND substr("EventId",24,1)='-' AND length(replace("EventId",'-',''))=32 AND replace("EventId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_NotificationIntent_EventIdLength" CHECK ("EventId" IS NULL OR length("EventId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_NotificationIntent_FailedAtUtcMsRange" CHECK ("FailedAtUtcMs" IS NULL OR "FailedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_NotificationIntent_FilterSnapshotJsonLength" CHECK ("FilterSnapshotJson" IS NULL OR length("FilterSnapshotJson") BETWEEN 1 AND 2048),
    CONSTRAINT "CK_NotificationIntent_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_NotificationIntent_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_NotificationIntent_Json" CHECK (CASE WHEN json_valid(FilterSnapshotJson) THEN json_type(FilterSnapshotJson)='object' ELSE 0 END),
    CONSTRAINT "CK_NotificationIntent_LastErrorCodeLength" CHECK ("LastErrorCode" IS NULL OR length("LastErrorCode") BETWEEN 1 AND 64),
    CONSTRAINT "CK_NotificationIntent_LastErrorSummaryLength" CHECK ("LastErrorSummary" IS NULL OR length("LastErrorSummary") BETWEEN 1 AND 500),
    CONSTRAINT "CK_NotificationIntent_LeaseExpiresAtUtcMsRange" CHECK ("LeaseExpiresAtUtcMs" IS NULL OR "LeaseExpiresAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_NotificationIntent_MatchedAtUtcMsRange" CHECK ("MatchedAtUtcMs" IS NULL OR "MatchedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_NotificationIntent_NextAttemptAtUtcMsRange" CHECK ("NextAttemptAtUtcMs" IS NULL OR "NextAttemptAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_NotificationIntent_OwnerIdGuid" CHECK ("OwnerId" IS NULL OR (length("OwnerId")=36 AND substr("OwnerId",9,1)='-' AND substr("OwnerId",14,1)='-' AND substr("OwnerId",19,1)='-' AND substr("OwnerId",24,1)='-' AND length(replace("OwnerId",'-',''))=32 AND replace("OwnerId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_NotificationIntent_OwnerIdLength" CHECK ("OwnerId" IS NULL OR length("OwnerId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_NotificationIntent_PayloadSchemaVersionPositive" CHECK ("PayloadSchemaVersion" IS NULL OR "PayloadSchemaVersion">=1),
    CONSTRAINT "CK_NotificationIntent_PredicateVersionPositive" CHECK ("PredicateVersion" IS NULL OR "PredicateVersion">=1),
    CONSTRAINT "CK_NotificationIntent_RecipientAddressSnapshotLength" CHECK ("RecipientAddressSnapshot" IS NULL OR length("RecipientAddressSnapshot") BETWEEN 1 AND 320),
    CONSTRAINT "CK_NotificationIntent_RuleIdGuid" CHECK ("RuleId" IS NULL OR (length("RuleId")=36 AND substr("RuleId",9,1)='-' AND substr("RuleId",14,1)='-' AND substr("RuleId",19,1)='-' AND substr("RuleId",24,1)='-' AND length(replace("RuleId",'-',''))=32 AND replace("RuleId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_NotificationIntent_RuleIdLength" CHECK ("RuleId" IS NULL OR length("RuleId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_NotificationIntent_RuleNameSnapshotLength" CHECK ("RuleNameSnapshot" IS NULL OR length("RuleNameSnapshot") BETWEEN 1 AND 100),
    CONSTRAINT "CK_NotificationIntent_RuleVersionPositive" CHECK ("RuleVersion" IS NULL OR "RuleVersion">=1),
    CONSTRAINT "CK_NotificationIntent_SecretReferenceSnapshotLength" CHECK ("SecretReferenceSnapshot" IS NULL OR length("SecretReferenceSnapshot") BETWEEN 1 AND 256),
    CONSTRAINT "CK_NotificationIntent_State" CHECK ((Status='Pending' AND AttemptCount=0 AND NextAttemptAtUtcMs IS NOT NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='InFlight' AND AttemptCount BETWEEN 1 AND 5 AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NOT NULL AND LeaseExpiresAtUtcMs IS NOT NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='Delivered' AND AttemptCount BETWEEN 1 AND 5 AND DeliveredAtUtcMs IS NOT NULL AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='RetryScheduled' AND AttemptCount BETWEEN 1 AND 4 AND NextAttemptAtUtcMs IS NOT NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='Failed' AND FailedAtUtcMs IS NOT NULL AND LastErrorCode IS NOT NULL AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND (AttemptCount BETWEEN 1 AND 5 OR (AttemptCount=0 AND LastErrorCode='PermanentProvisioningFailure')))),
    CONSTRAINT "CK_NotificationIntent_StatusLength" CHECK ("Status" IS NULL OR length("Status") BETWEEN 1 AND 14),
    CONSTRAINT "CK_NotificationIntent_SubjectSnapshotLength" CHECK ("SubjectSnapshot" IS NULL OR length("SubjectSnapshot") BETWEEN 1 AND 200),
    CONSTRAINT "CK_NotificationIntent_VersionPositive" CHECK ("Version" IS NULL OR "Version">=1),
    CONSTRAINT "FK_NotificationIntent_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_NotificationIntent_ChannelDestination_DestinationId_OwnerId_Channel" FOREIGN KEY ("DestinationId", "OwnerId", "Channel") REFERENCES "ChannelDestination" ("Id", "OwnerId", "Channel") ON DELETE RESTRICT,
    CONSTRAINT "FK_NotificationIntent_Event_EventId" FOREIGN KEY ("EventId") REFERENCES "Event" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_NotificationIntent_Rule_RuleId_OwnerId" FOREIGN KEY ("RuleId", "OwnerId") REFERENCES "Rule" ("Id", "OwnerId") ON DELETE RESTRICT
);

CREATE TABLE "SeenSourceItem" (
    "SourceId" TEXT COLLATE BINARY NOT NULL,
    "ExternalKey" TEXT COLLATE BINARY NOT NULL,
    "Disposition" TEXT COLLATE BINARY NOT NULL,
    "FirstSeenAtUtcMs" INTEGER NOT NULL,
    "PublishedAtUtcMs" INTEGER NULL,
    "EventId" TEXT COLLATE BINARY NULL,
    "PayloadFingerprint" BLOB NULL,
    CONSTRAINT "PK_SeenSourceItem" PRIMARY KEY ("SourceId", "ExternalKey"),
    CONSTRAINT "CK_SeenSourceItem_Disposition" CHECK ((Disposition='Accepted' AND EventId IS NOT NULL AND PayloadFingerprint IS NOT NULL) OR (Disposition IN ('BaselineSuppressed','OldSuppressed') AND EventId IS NULL)),
    CONSTRAINT "CK_SeenSourceItem_DispositionLength" CHECK ("Disposition" IS NULL OR length("Disposition") BETWEEN 1 AND 20),
    CONSTRAINT "CK_SeenSourceItem_EventIdGuid" CHECK ("EventId" IS NULL OR (length("EventId")=36 AND substr("EventId",9,1)='-' AND substr("EventId",14,1)='-' AND substr("EventId",19,1)='-' AND substr("EventId",24,1)='-' AND length(replace("EventId",'-',''))=32 AND replace("EventId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_SeenSourceItem_EventIdLength" CHECK ("EventId" IS NULL OR length("EventId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_SeenSourceItem_ExternalKeyLength" CHECK ("ExternalKey" IS NULL OR length("ExternalKey") BETWEEN 1 AND 12288),
    CONSTRAINT "CK_SeenSourceItem_FirstSeenAtUtcMsRange" CHECK ("FirstSeenAtUtcMs" IS NULL OR "FirstSeenAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SeenSourceItem_PayloadFingerprintHash" CHECK ("PayloadFingerprint" IS NULL OR length("PayloadFingerprint")=32),
    CONSTRAINT "CK_SeenSourceItem_PublishedAtUtcMsRange" CHECK ("PublishedAtUtcMs" IS NULL OR "PublishedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_SeenSourceItem_SourceIdGuid" CHECK ("SourceId" IS NULL OR (length("SourceId")=36 AND substr("SourceId",9,1)='-' AND substr("SourceId",14,1)='-' AND substr("SourceId",19,1)='-' AND substr("SourceId",24,1)='-' AND length(replace("SourceId",'-',''))=32 AND replace("SourceId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_SeenSourceItem_SourceIdLength" CHECK ("SourceId" IS NULL OR length("SourceId") BETWEEN 1 AND 36),
    CONSTRAINT "FK_SeenSourceItem_Event_EventId_SourceId_ExternalKey" FOREIGN KEY ("EventId", "SourceId", "ExternalKey") REFERENCES "Event" ("Id", "SourceId", "ExternalKey") ON DELETE RESTRICT,
    CONSTRAINT "FK_SeenSourceItem_SourceState_SourceId" FOREIGN KEY ("SourceId") REFERENCES "SourceState" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "DeliveryAttempt" (
    "Id" TEXT COLLATE BINARY NOT NULL CONSTRAINT "PK_DeliveryAttempt" PRIMARY KEY,
    "IntentId" TEXT COLLATE BINARY NOT NULL,
    "Ordinal" INTEGER NOT NULL,
    "ClaimToken" TEXT COLLATE BINARY NOT NULL,
    "StartedAtUtcMs" INTEGER NOT NULL,
    "LeaseExpiresAtUtcMs" INTEGER NOT NULL,
    "FinishedAtUtcMs" INTEGER NULL,
    "Outcome" TEXT COLLATE BINARY NOT NULL,
    "ErrorCode" TEXT COLLATE BINARY NULL,
    "ErrorSummary" TEXT COLLATE BINARY NULL,
    "HttpStatus" INTEGER NULL,
    "SmtpStatus" INTEGER NULL,
    "RetryAfterMs" INTEGER NULL,
    CONSTRAINT "CK_DeliveryAttempt_ClaimTokenGuid" CHECK ("ClaimToken" IS NULL OR (length("ClaimToken")=36 AND substr("ClaimToken",9,1)='-' AND substr("ClaimToken",14,1)='-' AND substr("ClaimToken",19,1)='-' AND substr("ClaimToken",24,1)='-' AND length(replace("ClaimToken",'-',''))=32 AND replace("ClaimToken",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_DeliveryAttempt_ClaimTokenLength" CHECK ("ClaimToken" IS NULL OR length("ClaimToken") BETWEEN 1 AND 36),
    CONSTRAINT "CK_DeliveryAttempt_ErrorCodeLength" CHECK ("ErrorCode" IS NULL OR length("ErrorCode") BETWEEN 1 AND 64),
    CONSTRAINT "CK_DeliveryAttempt_ErrorSummaryLength" CHECK ("ErrorSummary" IS NULL OR length("ErrorSummary") BETWEEN 1 AND 500),
    CONSTRAINT "CK_DeliveryAttempt_FinishedAtUtcMsRange" CHECK ("FinishedAtUtcMs" IS NULL OR "FinishedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_DeliveryAttempt_HttpStatusRange" CHECK ("HttpStatus" IS NULL OR "HttpStatus" BETWEEN 100 AND 599),
    CONSTRAINT "CK_DeliveryAttempt_IdGuid" CHECK ("Id" IS NULL OR (length("Id")=36 AND substr("Id",9,1)='-' AND substr("Id",14,1)='-' AND substr("Id",19,1)='-' AND substr("Id",24,1)='-' AND length(replace("Id",'-',''))=32 AND replace("Id",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_DeliveryAttempt_IdLength" CHECK ("Id" IS NULL OR length("Id") BETWEEN 1 AND 36),
    CONSTRAINT "CK_DeliveryAttempt_IntentIdGuid" CHECK ("IntentId" IS NULL OR (length("IntentId")=36 AND substr("IntentId",9,1)='-' AND substr("IntentId",14,1)='-' AND substr("IntentId",19,1)='-' AND substr("IntentId",24,1)='-' AND length(replace("IntentId",'-',''))=32 AND replace("IntentId",'-','') NOT GLOB '*[^0-9a-f]*')),
    CONSTRAINT "CK_DeliveryAttempt_IntentIdLength" CHECK ("IntentId" IS NULL OR length("IntentId") BETWEEN 1 AND 36),
    CONSTRAINT "CK_DeliveryAttempt_LeaseExpiresAtUtcMsRange" CHECK ("LeaseExpiresAtUtcMs" IS NULL OR "LeaseExpiresAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_DeliveryAttempt_OutcomeLength" CHECK ("Outcome" IS NULL OR length("Outcome") BETWEEN 1 AND 20),
    CONSTRAINT "CK_DeliveryAttempt_Retry" CHECK (RetryAfterMs IS NULL OR RetryAfterMs>=0),
    CONSTRAINT "CK_DeliveryAttempt_SmtpStatusRange" CHECK ("SmtpStatus" IS NULL OR "SmtpStatus" BETWEEN 100 AND 599),
    CONSTRAINT "CK_DeliveryAttempt_StartedAtUtcMsRange" CHECK ("StartedAtUtcMs" IS NULL OR "StartedAtUtcMs" BETWEEN -62135596800000 AND 253402300799999),
    CONSTRAINT "CK_DeliveryAttempt_State" CHECK (Ordinal BETWEEN 1 AND 5 AND LeaseExpiresAtUtcMs>StartedAtUtcMs AND ((Outcome='InFlight' AND FinishedAtUtcMs IS NULL) OR (Outcome IN ('Succeeded','TransientFailure','PermanentFailure','OutcomeUnknown') AND FinishedAtUtcMs IS NOT NULL AND FinishedAtUtcMs>=StartedAtUtcMs))),
    CONSTRAINT "FK_DeliveryAttempt_NotificationIntent_IntentId" FOREIGN KEY ("IntentId") REFERENCES "NotificationIntent" ("Id") ON DELETE RESTRICT
);

INSERT INTO "AspNetRoles" ("Id", "ConcurrencyStamp", "Name", "NormalizedName")
VALUES ('00000000-0000-0000-0000-000000000001', 'role-user-v1', 'User', 'USER');
SELECT changes();

INSERT INTO "AspNetRoles" ("Id", "ConcurrencyStamp", "Name", "NormalizedName")
VALUES ('00000000-0000-0000-0000-000000000002', 'role-admin-v1', 'Admin', 'ADMIN');
SELECT changes();


CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE UNIQUE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE UNIQUE INDEX "IX_ChannelDestination_OwnerId_Channel" ON "ChannelDestination" ("OwnerId", "Channel") WHERE IsAvailable=1;

CREATE UNIQUE INDEX "IX_ChannelDestination_OwnerId_Channel_ConfigurationVersion" ON "ChannelDestination" ("OwnerId", "Channel", "ConfigurationVersion");

CREATE UNIQUE INDEX "IX_DeliveryAttempt_ClaimToken" ON "DeliveryAttempt" ("ClaimToken");

CREATE UNIQUE INDEX "IX_DeliveryAttempt_IntentId" ON "DeliveryAttempt" ("IntentId") WHERE Outcome='InFlight';

CREATE UNIQUE INDEX "IX_DeliveryAttempt_IntentId_Ordinal" ON "DeliveryAttempt" ("IntentId", "Ordinal");

CREATE INDEX "IX_Event_AcceptedAtUtcMs_Id" ON "Event" ("AcceptedAtUtcMs" DESC, "Id" DESC);

CREATE INDEX "IX_Event_Category_AcceptedAtUtcMs_Id" ON "Event" ("Category", "AcceptedAtUtcMs" DESC, "Id" DESC);

CREATE INDEX "IX_Event_SourceId_AcceptedAtUtcMs_Id" ON "Event" ("SourceId", "AcceptedAtUtcMs" DESC, "Id" DESC);

CREATE INDEX "IX_Event_SourceId_Category_Provenance" ON "Event" ("SourceId", "Category", "Provenance");

CREATE UNIQUE INDEX "IX_Event_SourceId_ExternalKey" ON "Event" ("SourceId", "ExternalKey");

CREATE INDEX "IX_Event_SubmittedByUserId" ON "Event" ("SubmittedByUserId");

CREATE INDEX "IX_NotificationIntent_CreatedAtUtcMs_Id" ON "NotificationIntent" ("CreatedAtUtcMs" DESC, "Id" DESC);

CREATE INDEX "IX_NotificationIntent_DestinationId_OwnerId_Channel" ON "NotificationIntent" ("DestinationId", "OwnerId", "Channel");

CREATE INDEX "IX_NotificationIntent_EventId_Id" ON "NotificationIntent" ("EventId", "Id");

CREATE UNIQUE INDEX "IX_NotificationIntent_EventId_RuleId_Channel" ON "NotificationIntent" ("EventId", "RuleId", "Channel");

CREATE INDEX "IX_NotificationIntent_OwnerId_CreatedAtUtcMs_Id" ON "NotificationIntent" ("OwnerId", "CreatedAtUtcMs" DESC, "Id" DESC);

CREATE INDEX "IX_NotificationIntent_RuleId_OwnerId" ON "NotificationIntent" ("RuleId", "OwnerId");

CREATE INDEX "IX_NotificationIntent_Status_LeaseExpiresAtUtcMs" ON "NotificationIntent" ("Status", "LeaseExpiresAtUtcMs");

CREATE INDEX "IX_NotificationIntent_Status_NextAttemptAtUtcMs_CreatedAtUtcMs_Id" ON "NotificationIntent" ("Status", "NextAttemptAtUtcMs", "CreatedAtUtcMs", "Id");

CREATE INDEX "IX_Rule_Category_Enabled_DeletedAtUtcMs" ON "Rule" ("Category", "Enabled", "DeletedAtUtcMs");

CREATE INDEX "IX_Rule_OwnerId_DeletedAtUtcMs_CreatedAtUtcMs_Id" ON "Rule" ("OwnerId", "DeletedAtUtcMs", "CreatedAtUtcMs" DESC, "Id" DESC);

CREATE UNIQUE INDEX "IX_SeenSourceItem_EventId" ON "SeenSourceItem" ("EventId") WHERE EventId IS NOT NULL;

CREATE INDEX "IX_SeenSourceItem_EventId_SourceId_ExternalKey" ON "SeenSourceItem" ("EventId", "SourceId", "ExternalKey");

CREATE INDEX "IX_SeenSourceItem_SourceId_FirstSeenAtUtcMs" ON "SeenSourceItem" ("SourceId", "FirstSeenAtUtcMs");

CREATE INDEX "IX_SourceFailure_SourceId_OccurredAtUtcMs_Id" ON "SourceFailure" ("SourceId", "OccurredAtUtcMs" DESC, "Id" DESC);

CREATE INDEX "IX_SourceState_Enabled_NextPollAtUtcMs" ON "SourceState" ("Enabled", "NextPollAtUtcMs");

CREATE UNIQUE INDEX "IX_SourceState_LogicalName_ConfigurationVersion" ON "SourceState" ("LogicalName", "ConfigurationVersion");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261027131254_InitialPersistence', '10.0.14');

COMMIT;
