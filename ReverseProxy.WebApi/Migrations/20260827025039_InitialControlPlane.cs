using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReverseProxy.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialControlPlane : Migration
    {
        private static readonly string[] OutboxEventRevisionColumns = ["event_type", "revision_id"];
        private static readonly string[] OutboxPublishedOccurredColumns = ["published_at", "occurred_at"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    revision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auth_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "config_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<long>(type: "bigint", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "config_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<long>(type: "bigint", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    published_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_revisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_protection_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "runtime_nodes",
                columns: table => new
                {
                    node_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    applied_revision_number = table.Column<long>(type: "bigint", nullable: false),
                    applied_content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runtime_nodes", x => x.node_id);
                });

            migrationBuilder.CreateTable(
                name: "auth_role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auth_role_claims_auth_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "auth_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auth_user_claims_auth_users_UserId",
                        column: x => x.UserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_auth_user_logins_auth_users_UserId",
                        column: x => x.UserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_auth_user_roles_auth_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "auth_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_auth_user_roles_auth_users_UserId",
                        column: x => x.UserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_user_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_auth_user_tokens_auth_users_UserId",
                        column: x => x.UserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "config_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    base_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_revision_number = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_drafts", x => x.id);
                    table.ForeignKey(
                        name: "FK_config_drafts_config_revisions_base_revision_id",
                        column: x => x.base_revision_id,
                        principalTable: "config_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "config_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<long>(type: "bigint", nullable: false),
                    requested_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_operations", x => x.id);
                    table.ForeignKey(
                        name: "FK_config_operations_config_revisions_revision_id",
                        column: x => x.revision_id,
                        principalTable: "config_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "runtime_config_state",
                columns: table => new
                {
                    state_key = table.Column<int>(type: "integer", nullable: false),
                    active_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_revision_number = table.Column<long>(type: "bigint", nullable: false),
                    active_content_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runtime_config_state", x => x.state_key);
                    table.CheckConstraint("ck_runtime_config_state_singleton", "state_key = 1");
                    table.ForeignKey(
                        name: "FK_runtime_config_state_config_revisions_active_revision_id",
                        column: x => x.active_revision_id,
                        principalTable: "config_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_occurred_at",
                table: "audit_entries",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_revision_id",
                table: "audit_entries",
                column: "revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_role_claims_RoleId",
                table: "auth_role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "auth_roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_user_claims_UserId",
                table: "auth_user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_auth_user_logins_UserId",
                table: "auth_user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_auth_user_roles_RoleId",
                table: "auth_user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "auth_users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "auth_users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_config_drafts_base_revision_id",
                table: "config_drafts",
                column: "base_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_config_drafts_name",
                table: "config_drafts",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_config_operations_created_at",
                table: "config_operations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_config_operations_revision_id",
                table: "config_operations",
                column: "revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_config_outbox_event_type_revision_id",
                table: "config_outbox",
                columns: OutboxEventRevisionColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_config_outbox_published_at_occurred_at",
                table: "config_outbox",
                columns: OutboxPublishedOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_config_revisions_revision_number",
                table: "config_revisions",
                column: "revision_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_runtime_config_state_active_revision_id",
                table: "runtime_config_state",
                column: "active_revision_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_runtime_nodes_last_seen_at",
                table: "runtime_nodes",
                column: "last_seen_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "auth_role_claims");

            migrationBuilder.DropTable(
                name: "auth_user_claims");

            migrationBuilder.DropTable(
                name: "auth_user_logins");

            migrationBuilder.DropTable(
                name: "auth_user_roles");

            migrationBuilder.DropTable(
                name: "auth_user_tokens");

            migrationBuilder.DropTable(
                name: "config_drafts");

            migrationBuilder.DropTable(
                name: "config_operations");

            migrationBuilder.DropTable(
                name: "config_outbox");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "runtime_config_state");

            migrationBuilder.DropTable(
                name: "runtime_nodes");

            migrationBuilder.DropTable(
                name: "auth_roles");

            migrationBuilder.DropTable(
                name: "auth_users");

            migrationBuilder.DropTable(
                name: "config_revisions");
        }
    }
}
