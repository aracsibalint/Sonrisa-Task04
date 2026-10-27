using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Alerts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                    table.CheckConstraint("CK_AspNetRoles_ConcurrencyStampLength", "\"ConcurrencyStamp\" IS NULL OR length(\"ConcurrencyStamp\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetRoles_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetRoles_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_AspNetRoles_NameLength", "\"Name\" IS NULL OR length(\"Name\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetRoles_NormalizedNameLength", "\"NormalizedName\" IS NULL OR length(\"NormalizedName\") BETWEEN 1 AND 256");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false, collation: "BINARY"),
                    SecurityStamp = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "BINARY"),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<long>(type: "INTEGER", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.CheckConstraint("CK_AspNetUsers_ConcurrencyStampLength", "\"ConcurrencyStamp\" IS NULL OR length(\"ConcurrencyStamp\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUsers_EmailConfirmedBool", "\"EmailConfirmed\" IS NULL OR \"EmailConfirmed\" IN (0,1)");
                    table.CheckConstraint("CK_AspNetUsers_EmailLength", "\"Email\" IS NULL OR length(\"Email\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUsers_FailedCount", "AccessFailedCount>=0");
                    table.CheckConstraint("CK_AspNetUsers_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetUsers_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_AspNetUsers_LockoutEnabledBool", "\"LockoutEnabled\" IS NULL OR \"LockoutEnabled\" IN (0,1)");
                    table.CheckConstraint("CK_AspNetUsers_LockoutEndRange", "\"LockoutEnd\" IS NULL OR \"LockoutEnd\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_AspNetUsers_NormalizedEmailLength", "\"NormalizedEmail\" IS NULL OR length(\"NormalizedEmail\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUsers_NormalizedUserNameLength", "\"NormalizedUserName\" IS NULL OR length(\"NormalizedUserName\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUsers_PasswordHashLength", "\"PasswordHash\" IS NULL OR length(\"PasswordHash\") BETWEEN 1 AND 2048");
                    table.CheckConstraint("CK_AspNetUsers_PhoneNumberConfirmedBool", "\"PhoneNumberConfirmed\" IS NULL OR \"PhoneNumberConfirmed\" IN (0,1)");
                    table.CheckConstraint("CK_AspNetUsers_PhoneNumberLength", "\"PhoneNumber\" IS NULL OR length(\"PhoneNumber\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_AspNetUsers_SecurityStampLength", "\"SecurityStamp\" IS NULL OR length(\"SecurityStamp\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUsers_TwoFactorEnabledBool", "\"TwoFactorEnabled\" IS NULL OR \"TwoFactorEnabled\" IN (0,1)");
                    table.CheckConstraint("CK_AspNetUsers_UserNameLength", "\"UserName\" IS NULL OR length(\"UserName\") BETWEEN 1 AND 256");
                });

            migrationBuilder.CreateTable(
                name: "SourceState",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    LogicalName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "BINARY"),
                    ConfigurationVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, collation: "BINARY"),
                    Category = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, collation: "BINARY"),
                    Provenance = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false, collation: "BINARY"),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "BINARY"),
                    ConfigurationReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    IdentityVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    ScheduleVersion = table.Column<long>(type: "INTEGER", nullable: true),
                    PollIntervalMs = table.Column<long>(type: "INTEGER", nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActivatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    Health = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, collation: "BINARY"),
                    BaselineCompletedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    ETag = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true, collation: "BINARY"),
                    LastModified = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true, collation: "BINARY"),
                    LastAttemptAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    LastFetchSucceededAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    LastValidObservationAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    NextPollAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    ConsecutiveFailures = table.Column<long>(type: "INTEGER", nullable: false),
                    ConsecutiveSchemaFailures = table.Column<long>(type: "INTEGER", nullable: false),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    LastErrorSummary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, collation: "BINARY"),
                    LastPollRejectedItems = table.Column<long>(type: "INTEGER", nullable: false),
                    LastPollConflictingItems = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceState", x => x.Id);
                    table.UniqueConstraint("AK_SourceState_Id_Category_Provenance", x => new { x.Id, x.Category, x.Provenance });
                    table.CheckConstraint("CK_SourceState_ActivatedAtUtcMsRange", "\"ActivatedAtUtcMs\" IS NULL OR \"ActivatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_Activation", "Enabled=0 OR ActivatedAtUtcMs IS NOT NULL");
                    table.CheckConstraint("CK_SourceState_Baseline", "Kind='Rss' OR (BaselineCompletedAtUtcMs IS NULL AND ETag IS NULL AND LastModified IS NULL)");
                    table.CheckConstraint("CK_SourceState_BaselineCompletedAtUtcMsRange", "\"BaselineCompletedAtUtcMs\" IS NULL OR \"BaselineCompletedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_CategoryLength", "\"Category\" IS NULL OR length(\"Category\") BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_SourceState_ConfigurationReferenceLength", "\"ConfigurationReference\" IS NULL OR length(\"ConfigurationReference\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_SourceState_ConfigurationVersionPositive", "\"ConfigurationVersion\" IS NULL OR \"ConfigurationVersion\">=1");
                    table.CheckConstraint("CK_SourceState_ConsecutiveFailuresCounter", "\"ConsecutiveFailures\">=0");
                    table.CheckConstraint("CK_SourceState_ConsecutiveSchemaFailuresCounter", "\"ConsecutiveSchemaFailures\">=0");
                    table.CheckConstraint("CK_SourceState_CreatedAtUtcMsRange", "\"CreatedAtUtcMs\" IS NULL OR \"CreatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_DisplayNameLength", "\"DisplayName\" IS NULL OR length(\"DisplayName\") BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_SourceState_EnabledBool", "\"Enabled\" IS NULL OR \"Enabled\" IN (0,1)");
                    table.CheckConstraint("CK_SourceState_ETagLength", "\"ETag\" IS NULL OR length(\"ETag\") BETWEEN 1 AND 1024");
                    table.CheckConstraint("CK_SourceState_Health", "Health IN ('Uninitialized','Healthy','Degraded','Blocked','Disabled')");
                    table.CheckConstraint("CK_SourceState_HealthLength", "\"Health\" IS NULL OR length(\"Health\") BETWEEN 1 AND 16");
                    table.CheckConstraint("CK_SourceState_IdentityVersionPositive", "\"IdentityVersion\" IS NULL OR \"IdentityVersion\">=1");
                    table.CheckConstraint("CK_SourceState_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_SourceState_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_SourceState_Kind", "(Kind='Rss' AND Category='News' AND Provenance='Live') OR (Kind='Market' AND Category='Market' AND Provenance='Live') OR (Kind='EarthquakeWebhook' AND Category='Earthquake' AND Provenance='Simulated') OR (Kind='Manual' AND Category IN ('News','Market','Earthquake') AND Provenance='Manual')");
                    table.CheckConstraint("CK_SourceState_KindLength", "\"Kind\" IS NULL OR length(\"Kind\") BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_SourceState_LastAttemptAtUtcMsRange", "\"LastAttemptAtUtcMs\" IS NULL OR \"LastAttemptAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_LastErrorCodeLength", "\"LastErrorCode\" IS NULL OR length(\"LastErrorCode\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_SourceState_LastErrorSummaryLength", "\"LastErrorSummary\" IS NULL OR length(\"LastErrorSummary\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_SourceState_LastFetchSucceededAtUtcMsRange", "\"LastFetchSucceededAtUtcMs\" IS NULL OR \"LastFetchSucceededAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_LastModifiedLength", "\"LastModified\" IS NULL OR length(\"LastModified\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_SourceState_LastPollConflictingItemsCounter", "\"LastPollConflictingItems\">=0");
                    table.CheckConstraint("CK_SourceState_LastPollRejectedItemsCounter", "\"LastPollRejectedItems\">=0");
                    table.CheckConstraint("CK_SourceState_LastValidObservationAtUtcMsRange", "\"LastValidObservationAtUtcMs\" IS NULL OR \"LastValidObservationAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_LogicalNameLength", "\"LogicalName\" IS NULL OR length(\"LogicalName\") BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_SourceState_NextPollAtUtcMsRange", "\"NextPollAtUtcMs\" IS NULL OR \"NextPollAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_ProvenanceLength", "\"Provenance\" IS NULL OR length(\"Provenance\") BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_SourceState_Schedule", "(Kind IN ('Rss','Market') AND ScheduleVersion IS NOT NULL AND PollIntervalMs IS NOT NULL AND PollIntervalMs>0) OR (Kind NOT IN ('Rss','Market') AND ScheduleVersion IS NULL AND PollIntervalMs IS NULL AND NextPollAtUtcMs IS NULL)");
                    table.CheckConstraint("CK_SourceState_ScheduleVersionPositive", "\"ScheduleVersion\" IS NULL OR \"ScheduleVersion\">=1");
                    table.CheckConstraint("CK_SourceState_UpdatedAtUtcMsRange", "\"UpdatedAtUtcMs\" IS NULL OR \"UpdatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceState_VersionPositive", "\"Version\" IS NULL OR \"Version\">=1");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    ClaimType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true, collation: "BINARY"),
                    ClaimValue = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true, collation: "BINARY")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.CheckConstraint("CK_AspNetRoleClaims_ClaimTypeLength", "\"ClaimType\" IS NULL OR length(\"ClaimType\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetRoleClaims_ClaimValueLength", "\"ClaimValue\" IS NULL OR length(\"ClaimValue\") BETWEEN 1 AND 2048");
                    table.CheckConstraint("CK_AspNetRoleClaims_RoleIdGuid", "\"RoleId\" IS NULL OR (length(\"RoleId\")=36 AND substr(\"RoleId\",9,1)='-' AND substr(\"RoleId\",14,1)='-' AND substr(\"RoleId\",19,1)='-' AND substr(\"RoleId\",24,1)='-' AND length(replace(\"RoleId\",'-',''))=32 AND replace(\"RoleId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetRoleClaims_RoleIdLength", "\"RoleId\" IS NULL OR length(\"RoleId\") BETWEEN 1 AND 36");
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    ClaimType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true, collation: "BINARY"),
                    ClaimValue = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true, collation: "BINARY")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.CheckConstraint("CK_AspNetUserClaims_ClaimTypeLength", "\"ClaimType\" IS NULL OR length(\"ClaimType\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUserClaims_ClaimValueLength", "\"ClaimValue\" IS NULL OR length(\"ClaimValue\") BETWEEN 1 AND 2048");
                    table.CheckConstraint("CK_AspNetUserClaims_UserIdGuid", "\"UserId\" IS NULL OR (length(\"UserId\")=36 AND substr(\"UserId\",9,1)='-' AND substr(\"UserId\",14,1)='-' AND substr(\"UserId\",19,1)='-' AND substr(\"UserId\",24,1)='-' AND length(replace(\"UserId\",'-',''))=32 AND replace(\"UserId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetUserClaims_UserIdLength", "\"UserId\" IS NULL OR length(\"UserId\") BETWEEN 1 AND 36");
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true, collation: "BINARY"),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.CheckConstraint("CK_AspNetUserLogins_LoginProviderLength", "\"LoginProvider\" IS NULL OR length(\"LoginProvider\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_AspNetUserLogins_ProviderDisplayNameLength", "\"ProviderDisplayName\" IS NULL OR length(\"ProviderDisplayName\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_AspNetUserLogins_ProviderKeyLength", "\"ProviderKey\" IS NULL OR length(\"ProviderKey\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_AspNetUserLogins_UserIdGuid", "\"UserId\" IS NULL OR (length(\"UserId\")=36 AND substr(\"UserId\",9,1)='-' AND substr(\"UserId\",14,1)='-' AND substr(\"UserId\",19,1)='-' AND substr(\"UserId\",24,1)='-' AND length(replace(\"UserId\",'-',''))=32 AND replace(\"UserId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetUserLogins_UserIdLength", "\"UserId\" IS NULL OR length(\"UserId\") BETWEEN 1 AND 36");
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.CheckConstraint("CK_AspNetUserRoles_RoleIdGuid", "\"RoleId\" IS NULL OR (length(\"RoleId\")=36 AND substr(\"RoleId\",9,1)='-' AND substr(\"RoleId\",14,1)='-' AND substr(\"RoleId\",19,1)='-' AND substr(\"RoleId\",24,1)='-' AND length(replace(\"RoleId\",'-',''))=32 AND replace(\"RoleId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetUserRoles_RoleIdLength", "\"RoleId\" IS NULL OR length(\"RoleId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_AspNetUserRoles_UserIdGuid", "\"UserId\" IS NULL OR (length(\"UserId\")=36 AND substr(\"UserId\",9,1)='-' AND substr(\"UserId\",14,1)='-' AND substr(\"UserId\",19,1)='-' AND substr(\"UserId\",24,1)='-' AND length(replace(\"UserId\",'-',''))=32 AND replace(\"UserId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetUserRoles_UserIdLength", "\"UserId\" IS NULL OR length(\"UserId\") BETWEEN 1 AND 36");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true, collation: "BINARY")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.CheckConstraint("CK_AspNetUserTokens_LoginProviderLength", "\"LoginProvider\" IS NULL OR length(\"LoginProvider\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_AspNetUserTokens_NameLength", "\"Name\" IS NULL OR length(\"Name\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_AspNetUserTokens_UserIdGuid", "\"UserId\" IS NULL OR (length(\"UserId\")=36 AND substr(\"UserId\",9,1)='-' AND substr(\"UserId\",14,1)='-' AND substr(\"UserId\",19,1)='-' AND substr(\"UserId\",24,1)='-' AND length(replace(\"UserId\",'-',''))=32 AND replace(\"UserId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_AspNetUserTokens_UserIdLength", "\"UserId\" IS NULL OR length(\"UserId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_AspNetUserTokens_ValueLength", "\"Value\" IS NULL OR length(\"Value\") BETWEEN 1 AND 4096");
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChannelDestination",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    OwnerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false, collation: "BINARY"),
                    ConfigurationVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    DestinationIdentity = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    Address = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true, collation: "BINARY"),
                    SecretReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true, collation: "BINARY"),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevokedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDestination", x => x.Id);
                    table.UniqueConstraint("AK_ChannelDestination_Id_OwnerId_Channel", x => new { x.Id, x.OwnerId, x.Channel });
                    table.CheckConstraint("CK_ChannelDestination_Address", "(Channel='Email' AND Address IS NOT NULL AND SecretReference IS NULL) OR (Channel='Slack' AND SecretReference IS NOT NULL AND Address IS NULL)");
                    table.CheckConstraint("CK_ChannelDestination_AddressLength", "\"Address\" IS NULL OR length(\"Address\") BETWEEN 1 AND 320");
                    table.CheckConstraint("CK_ChannelDestination_Availability", "IsAvailable=0 OR RevokedAtUtcMs IS NULL");
                    table.CheckConstraint("CK_ChannelDestination_Channel", "Channel IN ('Email','Slack')");
                    table.CheckConstraint("CK_ChannelDestination_ChannelLength", "\"Channel\" IS NULL OR length(\"Channel\") BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_ChannelDestination_ConfigurationVersionPositive", "\"ConfigurationVersion\" IS NULL OR \"ConfigurationVersion\">=1");
                    table.CheckConstraint("CK_ChannelDestination_CreatedAtUtcMsRange", "\"CreatedAtUtcMs\" IS NULL OR \"CreatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_ChannelDestination_DestinationIdentityLength", "\"DestinationIdentity\" IS NULL OR length(\"DestinationIdentity\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_ChannelDestination_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_ChannelDestination_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_ChannelDestination_IsAvailableBool", "\"IsAvailable\" IS NULL OR \"IsAvailable\" IN (0,1)");
                    table.CheckConstraint("CK_ChannelDestination_OwnerIdGuid", "\"OwnerId\" IS NULL OR (length(\"OwnerId\")=36 AND substr(\"OwnerId\",9,1)='-' AND substr(\"OwnerId\",14,1)='-' AND substr(\"OwnerId\",19,1)='-' AND substr(\"OwnerId\",24,1)='-' AND length(replace(\"OwnerId\",'-',''))=32 AND replace(\"OwnerId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_ChannelDestination_OwnerIdLength", "\"OwnerId\" IS NULL OR length(\"OwnerId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_ChannelDestination_RevokedAtUtcMsRange", "\"RevokedAtUtcMs\" IS NULL OR \"RevokedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_ChannelDestination_SecretReferenceLength", "\"SecretReference\" IS NULL OR length(\"SecretReference\") BETWEEN 1 AND 256");
                    table.ForeignKey(
                        name: "FK_ChannelDestination_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rule",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    OwnerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "BINARY"),
                    Category = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, collation: "BINARY"),
                    Keyword = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, collation: "BINARY"),
                    Instrument = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, collation: "BINARY"),
                    ComparisonKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true, collation: "BINARY"),
                    ThresholdPercentText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    MinimumMagnitudeText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    MagnitudeScale = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true, collation: "BINARY"),
                    EmailSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    SlackSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rule", x => x.Id);
                    table.UniqueConstraint("AK_Rule_Id_OwnerId", x => new { x.Id, x.OwnerId });
                    table.CheckConstraint("CK_Rule_CategoryLength", "\"Category\" IS NULL OR length(\"Category\") BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_Rule_ComparisonKindLength", "\"ComparisonKind\" IS NULL OR length(\"ComparisonKind\") BETWEEN 1 AND 32");
                    table.CheckConstraint("CK_Rule_CreatedAtUtcMsRange", "\"CreatedAtUtcMs\" IS NULL OR \"CreatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Rule_DeletedAtUtcMsRange", "\"DeletedAtUtcMs\" IS NULL OR \"DeletedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Rule_EmailSelectedBool", "\"EmailSelected\" IS NULL OR \"EmailSelected\" IN (0,1)");
                    table.CheckConstraint("CK_Rule_EnabledBool", "\"Enabled\" IS NULL OR \"Enabled\" IN (0,1)");
                    table.CheckConstraint("CK_Rule_Filter", "(Category='News' AND Keyword IS NOT NULL AND Instrument IS NULL AND ComparisonKind IS NULL AND ThresholdPercentText IS NULL AND MinimumMagnitudeText IS NULL AND MagnitudeScale IS NULL) OR (Category='Market' AND Keyword IS NULL AND Instrument IS NOT NULL AND Instrument='BTCUSDT' AND ComparisonKind IS NOT NULL AND ComparisonKind='binance-rolling-24h' AND ThresholdPercentText IS NOT NULL AND MinimumMagnitudeText IS NULL AND MagnitudeScale IS NULL) OR (Category='Earthquake' AND Keyword IS NULL AND Instrument IS NULL AND ComparisonKind IS NULL AND ThresholdPercentText IS NULL AND MinimumMagnitudeText IS NOT NULL AND MagnitudeScale IS NOT NULL AND MagnitudeScale='Mw')");
                    table.CheckConstraint("CK_Rule_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_Rule_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_Rule_InstrumentLength", "\"Instrument\" IS NULL OR length(\"Instrument\") BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_Rule_KeywordLength", "\"Keyword\" IS NULL OR length(\"Keyword\") BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_Rule_MagnitudeScaleLength", "\"MagnitudeScale\" IS NULL OR length(\"MagnitudeScale\") BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_Rule_MinimumMagnitudeTextLength", "\"MinimumMagnitudeText\" IS NULL OR length(\"MinimumMagnitudeText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Rule_NameLength", "\"Name\" IS NULL OR length(\"Name\") BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_Rule_OwnerIdGuid", "\"OwnerId\" IS NULL OR (length(\"OwnerId\")=36 AND substr(\"OwnerId\",9,1)='-' AND substr(\"OwnerId\",14,1)='-' AND substr(\"OwnerId\",19,1)='-' AND substr(\"OwnerId\",24,1)='-' AND length(replace(\"OwnerId\",'-',''))=32 AND replace(\"OwnerId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_Rule_OwnerIdLength", "\"OwnerId\" IS NULL OR length(\"OwnerId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_Rule_Selection", "(EmailSelected=1 OR SlackSelected=1) AND (DeletedAtUtcMs IS NULL OR Enabled=0)");
                    table.CheckConstraint("CK_Rule_SlackSelectedBool", "\"SlackSelected\" IS NULL OR \"SlackSelected\" IN (0,1)");
                    table.CheckConstraint("CK_Rule_ThresholdPercentTextLength", "\"ThresholdPercentText\" IS NULL OR length(\"ThresholdPercentText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Rule_UpdatedAtUtcMsRange", "\"UpdatedAtUtcMs\" IS NULL OR \"UpdatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Rule_VersionPositive", "\"Version\" IS NULL OR \"Version\">=1");
                    table.ForeignKey(
                        name: "FK_Rule_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    SourceId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    ExternalKey = table.Column<string>(type: "TEXT", maxLength: 12288, nullable: false, collation: "BINARY"),
                    Category = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, collation: "BINARY"),
                    Provenance = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false, collation: "BINARY"),
                    SubmittedByUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true, collation: "BINARY"),
                    SchemaVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    FingerprintVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    PayloadFingerprint = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OccurredAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceivedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    AcceptedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    PredicateVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    EvaluatedRuleCount = table.Column<long>(type: "INTEGER", nullable: false),
                    MatchedRuleCount = table.Column<long>(type: "INTEGER", nullable: false),
                    IntentCount = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, collation: "BINARY"),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true, collation: "BINARY"),
                    ArticleUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true, collation: "BINARY"),
                    SummaryTruncated = table.Column<bool>(type: "INTEGER", nullable: true),
                    Instrument = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, collation: "BINARY"),
                    QuoteCurrency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true, collation: "BINARY"),
                    ComparisonKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true, collation: "BINARY"),
                    WindowStartUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    WindowEndUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    ScheduledSlotUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    OpenPriceText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    LastPriceText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    SignedMovementPercentText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    ProviderMovementPercentText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    MagnitudeText = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    MagnitudeScale = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true, collation: "BINARY"),
                    Place = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, collation: "BINARY"),
                    Simulated = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                    table.UniqueConstraint("AK_Event_Id_SourceId_ExternalKey", x => new { x.Id, x.SourceId, x.ExternalKey });
                    table.CheckConstraint("CK_Event_AcceptedAtUtcMsRange", "\"AcceptedAtUtcMs\" IS NULL OR \"AcceptedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Event_ArticleUrlLength", "\"ArticleUrl\" IS NULL OR length(\"ArticleUrl\") BETWEEN 1 AND 2048");
                    table.CheckConstraint("CK_Event_CategoryFields", "(Category='News' AND Title IS NOT NULL AND SummaryTruncated IS NOT NULL AND Instrument IS NULL AND QuoteCurrency IS NULL AND ComparisonKind IS NULL AND WindowStartUtcMs IS NULL AND WindowEndUtcMs IS NULL AND ScheduledSlotUtcMs IS NULL AND OpenPriceText IS NULL AND LastPriceText IS NULL AND SignedMovementPercentText IS NULL AND ProviderMovementPercentText IS NULL AND MagnitudeText IS NULL AND MagnitudeScale IS NULL AND Place IS NULL AND Simulated IS NULL) OR (Category='Market' AND Title IS NULL AND Summary IS NULL AND ArticleUrl IS NULL AND SummaryTruncated IS NULL AND MagnitudeText IS NULL AND MagnitudeScale IS NULL AND Place IS NULL AND Simulated IS NULL AND Instrument IS NOT NULL AND Instrument='BTCUSDT' AND QuoteCurrency IS NOT NULL AND QuoteCurrency='USDT' AND ComparisonKind IS NOT NULL AND ComparisonKind='binance-rolling-24h' AND WindowStartUtcMs IS NOT NULL AND WindowEndUtcMs IS NOT NULL AND WindowEndUtcMs-WindowStartUtcMs=86400000 AND OccurredAtUtcMs=WindowEndUtcMs AND OpenPriceText IS NOT NULL AND LastPriceText IS NOT NULL AND SignedMovementPercentText IS NOT NULL AND ((Provenance='Live' AND ScheduledSlotUtcMs IS NOT NULL AND ScheduledSlotUtcMs%60000=0) OR (Provenance='Manual' AND ScheduledSlotUtcMs IS NULL))) OR (Category='Earthquake' AND Title IS NULL AND Summary IS NULL AND ArticleUrl IS NULL AND SummaryTruncated IS NULL AND Instrument IS NULL AND QuoteCurrency IS NULL AND ComparisonKind IS NULL AND WindowStartUtcMs IS NULL AND WindowEndUtcMs IS NULL AND ScheduledSlotUtcMs IS NULL AND OpenPriceText IS NULL AND LastPriceText IS NULL AND SignedMovementPercentText IS NULL AND ProviderMovementPercentText IS NULL AND MagnitudeText IS NOT NULL AND MagnitudeScale IS NOT NULL AND MagnitudeScale='Mw' AND Simulated IS NOT NULL AND Simulated=1)");
                    table.CheckConstraint("CK_Event_CategoryLength", "\"Category\" IS NULL OR length(\"Category\") BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_Event_ComparisonKindLength", "\"ComparisonKind\" IS NULL OR length(\"ComparisonKind\") BETWEEN 1 AND 32");
                    table.CheckConstraint("CK_Event_Counts", "EvaluatedRuleCount BETWEEN 0 AND 100 AND MatchedRuleCount BETWEEN 0 AND EvaluatedRuleCount AND IntentCount BETWEEN MatchedRuleCount AND 2*MatchedRuleCount AND IntentCount<=200");
                    table.CheckConstraint("CK_Event_ExternalKeyLength", "\"ExternalKey\" IS NULL OR length(\"ExternalKey\") BETWEEN 1 AND 12288");
                    table.CheckConstraint("CK_Event_FingerprintVersionPositive", "\"FingerprintVersion\" IS NULL OR \"FingerprintVersion\">=1");
                    table.CheckConstraint("CK_Event_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_Event_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_Event_InstrumentLength", "\"Instrument\" IS NULL OR length(\"Instrument\") BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_Event_LastPriceTextLength", "\"LastPriceText\" IS NULL OR length(\"LastPriceText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Event_MagnitudeScaleLength", "\"MagnitudeScale\" IS NULL OR length(\"MagnitudeScale\") BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_Event_MagnitudeTextLength", "\"MagnitudeText\" IS NULL OR length(\"MagnitudeText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Event_Manual", "(Provenance='Manual' AND SubmittedByUserId IS NOT NULL) OR (Provenance IN ('Live','Simulated') AND SubmittedByUserId IS NULL)");
                    table.CheckConstraint("CK_Event_OccurredAtUtcMsRange", "\"OccurredAtUtcMs\" IS NULL OR \"OccurredAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Event_OpenPriceTextLength", "\"OpenPriceText\" IS NULL OR length(\"OpenPriceText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Event_PayloadFingerprintHash", "\"PayloadFingerprint\" IS NULL OR length(\"PayloadFingerprint\")=32");
                    table.CheckConstraint("CK_Event_PlaceLength", "\"Place\" IS NULL OR length(\"Place\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_Event_PredicateVersionPositive", "\"PredicateVersion\" IS NULL OR \"PredicateVersion\">=1");
                    table.CheckConstraint("CK_Event_ProvenanceLength", "\"Provenance\" IS NULL OR length(\"Provenance\") BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_Event_ProviderMovementPercentTextLength", "\"ProviderMovementPercentText\" IS NULL OR length(\"ProviderMovementPercentText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Event_QuoteCurrencyLength", "\"QuoteCurrency\" IS NULL OR length(\"QuoteCurrency\") BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_Event_ReceivedAtUtcMsRange", "\"ReceivedAtUtcMs\" IS NULL OR \"ReceivedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Event_ScheduledSlotUtcMsRange", "\"ScheduledSlotUtcMs\" IS NULL OR \"ScheduledSlotUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Event_SchemaVersionPositive", "\"SchemaVersion\" IS NULL OR \"SchemaVersion\">=1");
                    table.CheckConstraint("CK_Event_SignedMovementPercentTextLength", "\"SignedMovementPercentText\" IS NULL OR length(\"SignedMovementPercentText\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_Event_SimulatedBool", "\"Simulated\" IS NULL OR \"Simulated\" IN (0,1)");
                    table.CheckConstraint("CK_Event_SourceIdGuid", "\"SourceId\" IS NULL OR (length(\"SourceId\")=36 AND substr(\"SourceId\",9,1)='-' AND substr(\"SourceId\",14,1)='-' AND substr(\"SourceId\",19,1)='-' AND substr(\"SourceId\",24,1)='-' AND length(replace(\"SourceId\",'-',''))=32 AND replace(\"SourceId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_Event_SourceIdLength", "\"SourceId\" IS NULL OR length(\"SourceId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_Event_SubmittedByUserIdGuid", "\"SubmittedByUserId\" IS NULL OR (length(\"SubmittedByUserId\")=36 AND substr(\"SubmittedByUserId\",9,1)='-' AND substr(\"SubmittedByUserId\",14,1)='-' AND substr(\"SubmittedByUserId\",19,1)='-' AND substr(\"SubmittedByUserId\",24,1)='-' AND length(replace(\"SubmittedByUserId\",'-',''))=32 AND replace(\"SubmittedByUserId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_Event_SubmittedByUserIdLength", "\"SubmittedByUserId\" IS NULL OR length(\"SubmittedByUserId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_Event_SummaryLength", "\"Summary\" IS NULL OR length(\"Summary\") BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_Event_SummaryTruncatedBool", "\"SummaryTruncated\" IS NULL OR \"SummaryTruncated\" IN (0,1)");
                    table.CheckConstraint("CK_Event_TitleLength", "\"Title\" IS NULL OR length(\"Title\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_Event_WindowEndUtcMsRange", "\"WindowEndUtcMs\" IS NULL OR \"WindowEndUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_Event_WindowStartUtcMsRange", "\"WindowStartUtcMs\" IS NULL OR \"WindowStartUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.ForeignKey(
                        name: "FK_Event_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Event_SourceState_SourceId_Category_Provenance",
                        columns: x => new { x.SourceId, x.Category, x.Provenance },
                        principalTable: "SourceState",
                        principalColumns: new[] { "Id", "Category", "Provenance" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourceFailure",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    SourceId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    OccurredAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, collation: "BINARY"),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, collation: "BINARY"),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, collation: "BINARY"),
                    HttpStatus = table.Column<long>(type: "INTEGER", nullable: true),
                    RejectedItemCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceFailure", x => x.Id);
                    table.CheckConstraint("CK_SourceFailure_CodeLength", "\"Code\" IS NULL OR length(\"Code\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_SourceFailure_HttpStatusRange", "\"HttpStatus\" IS NULL OR \"HttpStatus\" BETWEEN 100 AND 599");
                    table.CheckConstraint("CK_SourceFailure_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_SourceFailure_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_SourceFailure_OccurredAtUtcMsRange", "\"OccurredAtUtcMs\" IS NULL OR \"OccurredAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SourceFailure_RejectedItemCountCounter", "\"RejectedItemCount\">=0");
                    table.CheckConstraint("CK_SourceFailure_SourceIdGuid", "\"SourceId\" IS NULL OR (length(\"SourceId\")=36 AND substr(\"SourceId\",9,1)='-' AND substr(\"SourceId\",14,1)='-' AND substr(\"SourceId\",19,1)='-' AND substr(\"SourceId\",24,1)='-' AND length(replace(\"SourceId\",'-',''))=32 AND replace(\"SourceId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_SourceFailure_SourceIdLength", "\"SourceId\" IS NULL OR length(\"SourceId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_SourceFailure_Stage", "Stage IN ('Fetch','Parse','Normalize','Accept','Schedule')");
                    table.CheckConstraint("CK_SourceFailure_StageLength", "\"Stage\" IS NULL OR length(\"Stage\") BETWEEN 1 AND 16");
                    table.CheckConstraint("CK_SourceFailure_SummaryLength", "\"Summary\" IS NULL OR length(\"Summary\") BETWEEN 1 AND 500");
                    table.ForeignKey(
                        name: "FK_SourceFailure_SourceState_SourceId",
                        column: x => x.SourceId,
                        principalTable: "SourceState",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationIntent",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    RuleId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    OwnerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    RuleVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    RuleNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "BINARY"),
                    Category = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, collation: "BINARY"),
                    FilterSnapshotJson = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false, collation: "BINARY"),
                    PredicateVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    MatchedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false, collation: "BINARY"),
                    DestinationId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    DestinationConfigurationVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    DestinationIdentitySnapshot = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "BINARY"),
                    RecipientAddressSnapshot = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true, collation: "BINARY"),
                    SecretReferenceSnapshot = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true, collation: "BINARY"),
                    SubjectSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, collation: "BINARY"),
                    BodySnapshot = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false, collation: "BINARY"),
                    PayloadSchemaVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false, collation: "BINARY"),
                    AttemptCount = table.Column<long>(type: "INTEGER", nullable: false),
                    NextAttemptAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    ClaimToken = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true, collation: "BINARY"),
                    LeaseExpiresAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    DeliveredAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    FailedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    LastErrorSummary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, collation: "BINARY"),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationIntent", x => x.Id);
                    table.CheckConstraint("CK_NotificationIntent_BodySnapshotLength", "\"BodySnapshot\" IS NULL OR length(\"BodySnapshot\") BETWEEN 1 AND 16000");
                    table.CheckConstraint("CK_NotificationIntent_Category", "Category IN ('News','Market','Earthquake')");
                    table.CheckConstraint("CK_NotificationIntent_CategoryLength", "\"Category\" IS NULL OR length(\"Category\") BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_NotificationIntent_Channel", "(Channel='Email' AND RecipientAddressSnapshot IS NOT NULL AND SubjectSnapshot IS NOT NULL AND SecretReferenceSnapshot IS NULL) OR (Channel='Slack' AND SecretReferenceSnapshot IS NOT NULL AND RecipientAddressSnapshot IS NULL AND SubjectSnapshot IS NULL)");
                    table.CheckConstraint("CK_NotificationIntent_ChannelLength", "\"Channel\" IS NULL OR length(\"Channel\") BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_NotificationIntent_ClaimTokenGuid", "\"ClaimToken\" IS NULL OR (length(\"ClaimToken\")=36 AND substr(\"ClaimToken\",9,1)='-' AND substr(\"ClaimToken\",14,1)='-' AND substr(\"ClaimToken\",19,1)='-' AND substr(\"ClaimToken\",24,1)='-' AND length(replace(\"ClaimToken\",'-',''))=32 AND replace(\"ClaimToken\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_NotificationIntent_ClaimTokenLength", "\"ClaimToken\" IS NULL OR length(\"ClaimToken\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_NotificationIntent_CreatedAtUtcMsRange", "\"CreatedAtUtcMs\" IS NULL OR \"CreatedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_NotificationIntent_DeliveredAtUtcMsRange", "\"DeliveredAtUtcMs\" IS NULL OR \"DeliveredAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_NotificationIntent_DestinationConfigurationVersionPositive", "\"DestinationConfigurationVersion\" IS NULL OR \"DestinationConfigurationVersion\">=1");
                    table.CheckConstraint("CK_NotificationIntent_DestinationIdentitySnapshotLength", "\"DestinationIdentitySnapshot\" IS NULL OR length(\"DestinationIdentitySnapshot\") BETWEEN 1 AND 128");
                    table.CheckConstraint("CK_NotificationIntent_DestinationIdGuid", "\"DestinationId\" IS NULL OR (length(\"DestinationId\")=36 AND substr(\"DestinationId\",9,1)='-' AND substr(\"DestinationId\",14,1)='-' AND substr(\"DestinationId\",19,1)='-' AND substr(\"DestinationId\",24,1)='-' AND length(replace(\"DestinationId\",'-',''))=32 AND replace(\"DestinationId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_NotificationIntent_DestinationIdLength", "\"DestinationId\" IS NULL OR length(\"DestinationId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_NotificationIntent_EventIdGuid", "\"EventId\" IS NULL OR (length(\"EventId\")=36 AND substr(\"EventId\",9,1)='-' AND substr(\"EventId\",14,1)='-' AND substr(\"EventId\",19,1)='-' AND substr(\"EventId\",24,1)='-' AND length(replace(\"EventId\",'-',''))=32 AND replace(\"EventId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_NotificationIntent_EventIdLength", "\"EventId\" IS NULL OR length(\"EventId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_NotificationIntent_FailedAtUtcMsRange", "\"FailedAtUtcMs\" IS NULL OR \"FailedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_NotificationIntent_FilterSnapshotJsonLength", "\"FilterSnapshotJson\" IS NULL OR length(\"FilterSnapshotJson\") BETWEEN 1 AND 2048");
                    table.CheckConstraint("CK_NotificationIntent_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_NotificationIntent_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_NotificationIntent_Json", "CASE WHEN json_valid(FilterSnapshotJson) THEN json_type(FilterSnapshotJson)='object' ELSE 0 END");
                    table.CheckConstraint("CK_NotificationIntent_LastErrorCodeLength", "\"LastErrorCode\" IS NULL OR length(\"LastErrorCode\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_NotificationIntent_LastErrorSummaryLength", "\"LastErrorSummary\" IS NULL OR length(\"LastErrorSummary\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_NotificationIntent_LeaseExpiresAtUtcMsRange", "\"LeaseExpiresAtUtcMs\" IS NULL OR \"LeaseExpiresAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_NotificationIntent_MatchedAtUtcMsRange", "\"MatchedAtUtcMs\" IS NULL OR \"MatchedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_NotificationIntent_NextAttemptAtUtcMsRange", "\"NextAttemptAtUtcMs\" IS NULL OR \"NextAttemptAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_NotificationIntent_OwnerIdGuid", "\"OwnerId\" IS NULL OR (length(\"OwnerId\")=36 AND substr(\"OwnerId\",9,1)='-' AND substr(\"OwnerId\",14,1)='-' AND substr(\"OwnerId\",19,1)='-' AND substr(\"OwnerId\",24,1)='-' AND length(replace(\"OwnerId\",'-',''))=32 AND replace(\"OwnerId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_NotificationIntent_OwnerIdLength", "\"OwnerId\" IS NULL OR length(\"OwnerId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_NotificationIntent_PayloadSchemaVersionPositive", "\"PayloadSchemaVersion\" IS NULL OR \"PayloadSchemaVersion\">=1");
                    table.CheckConstraint("CK_NotificationIntent_PredicateVersionPositive", "\"PredicateVersion\" IS NULL OR \"PredicateVersion\">=1");
                    table.CheckConstraint("CK_NotificationIntent_RecipientAddressSnapshotLength", "\"RecipientAddressSnapshot\" IS NULL OR length(\"RecipientAddressSnapshot\") BETWEEN 1 AND 320");
                    table.CheckConstraint("CK_NotificationIntent_RuleIdGuid", "\"RuleId\" IS NULL OR (length(\"RuleId\")=36 AND substr(\"RuleId\",9,1)='-' AND substr(\"RuleId\",14,1)='-' AND substr(\"RuleId\",19,1)='-' AND substr(\"RuleId\",24,1)='-' AND length(replace(\"RuleId\",'-',''))=32 AND replace(\"RuleId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_NotificationIntent_RuleIdLength", "\"RuleId\" IS NULL OR length(\"RuleId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_NotificationIntent_RuleNameSnapshotLength", "\"RuleNameSnapshot\" IS NULL OR length(\"RuleNameSnapshot\") BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_NotificationIntent_RuleVersionPositive", "\"RuleVersion\" IS NULL OR \"RuleVersion\">=1");
                    table.CheckConstraint("CK_NotificationIntent_SecretReferenceSnapshotLength", "\"SecretReferenceSnapshot\" IS NULL OR length(\"SecretReferenceSnapshot\") BETWEEN 1 AND 256");
                    table.CheckConstraint("CK_NotificationIntent_State", "(Status='Pending' AND AttemptCount=0 AND NextAttemptAtUtcMs IS NOT NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='InFlight' AND AttemptCount BETWEEN 1 AND 5 AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NOT NULL AND LeaseExpiresAtUtcMs IS NOT NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='Delivered' AND AttemptCount BETWEEN 1 AND 5 AND DeliveredAtUtcMs IS NOT NULL AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='RetryScheduled' AND AttemptCount BETWEEN 1 AND 4 AND NextAttemptAtUtcMs IS NOT NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='Failed' AND FailedAtUtcMs IS NOT NULL AND LastErrorCode IS NOT NULL AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND (AttemptCount BETWEEN 1 AND 5 OR (AttemptCount=0 AND LastErrorCode='PermanentProvisioningFailure')))");
                    table.CheckConstraint("CK_NotificationIntent_StatusLength", "\"Status\" IS NULL OR length(\"Status\") BETWEEN 1 AND 14");
                    table.CheckConstraint("CK_NotificationIntent_SubjectSnapshotLength", "\"SubjectSnapshot\" IS NULL OR length(\"SubjectSnapshot\") BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_NotificationIntent_VersionPositive", "\"Version\" IS NULL OR \"Version\">=1");
                    table.ForeignKey(
                        name: "FK_NotificationIntent_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationIntent_ChannelDestination_DestinationId_OwnerId_Channel",
                        columns: x => new { x.DestinationId, x.OwnerId, x.Channel },
                        principalTable: "ChannelDestination",
                        principalColumns: new[] { "Id", "OwnerId", "Channel" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationIntent_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationIntent_Rule_RuleId_OwnerId",
                        columns: x => new { x.RuleId, x.OwnerId },
                        principalTable: "Rule",
                        principalColumns: new[] { "Id", "OwnerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeenSourceItem",
                columns: table => new
                {
                    SourceId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    ExternalKey = table.Column<string>(type: "TEXT", maxLength: 12288, nullable: false, collation: "BINARY"),
                    Disposition = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, collation: "BINARY"),
                    FirstSeenAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    PublishedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true, collation: "BINARY"),
                    PayloadFingerprint = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeenSourceItem", x => new { x.SourceId, x.ExternalKey });
                    table.CheckConstraint("CK_SeenSourceItem_Disposition", "(Disposition='Accepted' AND EventId IS NOT NULL AND PayloadFingerprint IS NOT NULL) OR (Disposition IN ('BaselineSuppressed','OldSuppressed') AND EventId IS NULL)");
                    table.CheckConstraint("CK_SeenSourceItem_DispositionLength", "\"Disposition\" IS NULL OR length(\"Disposition\") BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_SeenSourceItem_EventIdGuid", "\"EventId\" IS NULL OR (length(\"EventId\")=36 AND substr(\"EventId\",9,1)='-' AND substr(\"EventId\",14,1)='-' AND substr(\"EventId\",19,1)='-' AND substr(\"EventId\",24,1)='-' AND length(replace(\"EventId\",'-',''))=32 AND replace(\"EventId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_SeenSourceItem_EventIdLength", "\"EventId\" IS NULL OR length(\"EventId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_SeenSourceItem_ExternalKeyLength", "\"ExternalKey\" IS NULL OR length(\"ExternalKey\") BETWEEN 1 AND 12288");
                    table.CheckConstraint("CK_SeenSourceItem_FirstSeenAtUtcMsRange", "\"FirstSeenAtUtcMs\" IS NULL OR \"FirstSeenAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SeenSourceItem_PayloadFingerprintHash", "\"PayloadFingerprint\" IS NULL OR length(\"PayloadFingerprint\")=32");
                    table.CheckConstraint("CK_SeenSourceItem_PublishedAtUtcMsRange", "\"PublishedAtUtcMs\" IS NULL OR \"PublishedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_SeenSourceItem_SourceIdGuid", "\"SourceId\" IS NULL OR (length(\"SourceId\")=36 AND substr(\"SourceId\",9,1)='-' AND substr(\"SourceId\",14,1)='-' AND substr(\"SourceId\",19,1)='-' AND substr(\"SourceId\",24,1)='-' AND length(replace(\"SourceId\",'-',''))=32 AND replace(\"SourceId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_SeenSourceItem_SourceIdLength", "\"SourceId\" IS NULL OR length(\"SourceId\") BETWEEN 1 AND 36");
                    table.ForeignKey(
                        name: "FK_SeenSourceItem_Event_EventId_SourceId_ExternalKey",
                        columns: x => new { x.EventId, x.SourceId, x.ExternalKey },
                        principalTable: "Event",
                        principalColumns: new[] { "Id", "SourceId", "ExternalKey" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeenSourceItem_SourceState_SourceId",
                        column: x => x.SourceId,
                        principalTable: "SourceState",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAttempt",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    IntentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    Ordinal = table.Column<long>(type: "INTEGER", nullable: false),
                    ClaimToken = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false, collation: "BINARY"),
                    StartedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    LeaseExpiresAtUtcMs = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAtUtcMs = table.Column<long>(type: "INTEGER", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, collation: "BINARY"),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true, collation: "BINARY"),
                    ErrorSummary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, collation: "BINARY"),
                    HttpStatus = table.Column<long>(type: "INTEGER", nullable: true),
                    SmtpStatus = table.Column<long>(type: "INTEGER", nullable: true),
                    RetryAfterMs = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAttempt", x => x.Id);
                    table.CheckConstraint("CK_DeliveryAttempt_ClaimTokenGuid", "\"ClaimToken\" IS NULL OR (length(\"ClaimToken\")=36 AND substr(\"ClaimToken\",9,1)='-' AND substr(\"ClaimToken\",14,1)='-' AND substr(\"ClaimToken\",19,1)='-' AND substr(\"ClaimToken\",24,1)='-' AND length(replace(\"ClaimToken\",'-',''))=32 AND replace(\"ClaimToken\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_DeliveryAttempt_ClaimTokenLength", "\"ClaimToken\" IS NULL OR length(\"ClaimToken\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_DeliveryAttempt_ErrorCodeLength", "\"ErrorCode\" IS NULL OR length(\"ErrorCode\") BETWEEN 1 AND 64");
                    table.CheckConstraint("CK_DeliveryAttempt_ErrorSummaryLength", "\"ErrorSummary\" IS NULL OR length(\"ErrorSummary\") BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_DeliveryAttempt_FinishedAtUtcMsRange", "\"FinishedAtUtcMs\" IS NULL OR \"FinishedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_DeliveryAttempt_HttpStatusRange", "\"HttpStatus\" IS NULL OR \"HttpStatus\" BETWEEN 100 AND 599");
                    table.CheckConstraint("CK_DeliveryAttempt_IdGuid", "\"Id\" IS NULL OR (length(\"Id\")=36 AND substr(\"Id\",9,1)='-' AND substr(\"Id\",14,1)='-' AND substr(\"Id\",19,1)='-' AND substr(\"Id\",24,1)='-' AND length(replace(\"Id\",'-',''))=32 AND replace(\"Id\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_DeliveryAttempt_IdLength", "\"Id\" IS NULL OR length(\"Id\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_DeliveryAttempt_IntentIdGuid", "\"IntentId\" IS NULL OR (length(\"IntentId\")=36 AND substr(\"IntentId\",9,1)='-' AND substr(\"IntentId\",14,1)='-' AND substr(\"IntentId\",19,1)='-' AND substr(\"IntentId\",24,1)='-' AND length(replace(\"IntentId\",'-',''))=32 AND replace(\"IntentId\",'-','') NOT GLOB '*[^0-9a-f]*')");
                    table.CheckConstraint("CK_DeliveryAttempt_IntentIdLength", "\"IntentId\" IS NULL OR length(\"IntentId\") BETWEEN 1 AND 36");
                    table.CheckConstraint("CK_DeliveryAttempt_LeaseExpiresAtUtcMsRange", "\"LeaseExpiresAtUtcMs\" IS NULL OR \"LeaseExpiresAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_DeliveryAttempt_OutcomeLength", "\"Outcome\" IS NULL OR length(\"Outcome\") BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_DeliveryAttempt_Retry", "RetryAfterMs IS NULL OR RetryAfterMs>=0");
                    table.CheckConstraint("CK_DeliveryAttempt_SmtpStatusRange", "\"SmtpStatus\" IS NULL OR \"SmtpStatus\" BETWEEN 100 AND 599");
                    table.CheckConstraint("CK_DeliveryAttempt_StartedAtUtcMsRange", "\"StartedAtUtcMs\" IS NULL OR \"StartedAtUtcMs\" BETWEEN -62135596800000 AND 253402300799999");
                    table.CheckConstraint("CK_DeliveryAttempt_State", "Ordinal BETWEEN 1 AND 5 AND LeaseExpiresAtUtcMs>StartedAtUtcMs AND ((Outcome='InFlight' AND FinishedAtUtcMs IS NULL) OR (Outcome IN ('Succeeded','TransientFailure','PermanentFailure','OutcomeUnknown') AND FinishedAtUtcMs IS NOT NULL AND FinishedAtUtcMs>=StartedAtUtcMs))");
                    table.ForeignKey(
                        name: "FK_DeliveryAttempt_NotificationIntent_IntentId",
                        column: x => x.IntentId,
                        principalTable: "NotificationIntent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "00000000-0000-0000-0000-000000000001", "role-user-v1", "User", "USER" },
                    { "00000000-0000-0000-0000-000000000002", "role-admin-v1", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDestination_OwnerId_Channel",
                table: "ChannelDestination",
                columns: new[] { "OwnerId", "Channel" },
                unique: true,
                filter: "IsAvailable=1");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDestination_OwnerId_Channel_ConfigurationVersion",
                table: "ChannelDestination",
                columns: new[] { "OwnerId", "Channel", "ConfigurationVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempt_ClaimToken",
                table: "DeliveryAttempt",
                column: "ClaimToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempt_IntentId",
                table: "DeliveryAttempt",
                column: "IntentId",
                unique: true,
                filter: "Outcome='InFlight'");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempt_IntentId_Ordinal",
                table: "DeliveryAttempt",
                columns: new[] { "IntentId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_AcceptedAtUtcMs_Id",
                table: "Event",
                columns: new[] { "AcceptedAtUtcMs", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Event_Category_AcceptedAtUtcMs_Id",
                table: "Event",
                columns: new[] { "Category", "AcceptedAtUtcMs", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Event_SourceId_AcceptedAtUtcMs_Id",
                table: "Event",
                columns: new[] { "SourceId", "AcceptedAtUtcMs", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Event_SourceId_Category_Provenance",
                table: "Event",
                columns: new[] { "SourceId", "Category", "Provenance" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_SourceId_ExternalKey",
                table: "Event",
                columns: new[] { "SourceId", "ExternalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_SubmittedByUserId",
                table: "Event",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_CreatedAtUtcMs_Id",
                table: "NotificationIntent",
                columns: new[] { "CreatedAtUtcMs", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_DestinationId_OwnerId_Channel",
                table: "NotificationIntent",
                columns: new[] { "DestinationId", "OwnerId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_EventId_Id",
                table: "NotificationIntent",
                columns: new[] { "EventId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_EventId_RuleId_Channel",
                table: "NotificationIntent",
                columns: new[] { "EventId", "RuleId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_OwnerId_CreatedAtUtcMs_Id",
                table: "NotificationIntent",
                columns: new[] { "OwnerId", "CreatedAtUtcMs", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_RuleId_OwnerId",
                table: "NotificationIntent",
                columns: new[] { "RuleId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_Status_LeaseExpiresAtUtcMs",
                table: "NotificationIntent",
                columns: new[] { "Status", "LeaseExpiresAtUtcMs" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationIntent_Status_NextAttemptAtUtcMs_CreatedAtUtcMs_Id",
                table: "NotificationIntent",
                columns: new[] { "Status", "NextAttemptAtUtcMs", "CreatedAtUtcMs", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Rule_Category_Enabled_DeletedAtUtcMs",
                table: "Rule",
                columns: new[] { "Category", "Enabled", "DeletedAtUtcMs" });

            migrationBuilder.CreateIndex(
                name: "IX_Rule_OwnerId_DeletedAtUtcMs_CreatedAtUtcMs_Id",
                table: "Rule",
                columns: new[] { "OwnerId", "DeletedAtUtcMs", "CreatedAtUtcMs", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_SeenSourceItem_EventId",
                table: "SeenSourceItem",
                column: "EventId",
                unique: true,
                filter: "EventId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SeenSourceItem_EventId_SourceId_ExternalKey",
                table: "SeenSourceItem",
                columns: new[] { "EventId", "SourceId", "ExternalKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SeenSourceItem_SourceId_FirstSeenAtUtcMs",
                table: "SeenSourceItem",
                columns: new[] { "SourceId", "FirstSeenAtUtcMs" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceFailure_SourceId_OccurredAtUtcMs_Id",
                table: "SourceFailure",
                columns: new[] { "SourceId", "OccurredAtUtcMs", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_SourceState_Enabled_NextPollAtUtcMs",
                table: "SourceState",
                columns: new[] { "Enabled", "NextPollAtUtcMs" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceState_LogicalName_ConfigurationVersion",
                table: "SourceState",
                columns: new[] { "LogicalName", "ConfigurationVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DeliveryAttempt");

            migrationBuilder.DropTable(
                name: "SeenSourceItem");

            migrationBuilder.DropTable(
                name: "SourceFailure");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "NotificationIntent");

            migrationBuilder.DropTable(
                name: "ChannelDestination");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Rule");

            migrationBuilder.DropTable(
                name: "SourceState");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
