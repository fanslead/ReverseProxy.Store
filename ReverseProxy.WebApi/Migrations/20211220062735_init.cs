using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReverseProxy.WebApi.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoadBalancingPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clusters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionAffinityOptionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAffinityOptionSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Health = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Destinations_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AvailableDestinationsPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthCheckOptions_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProxyHttpClientOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SslProtocols = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DangerousAcceptAnyServerCertificate = table.Column<bool>(type: "bit", nullable: true),
                    MaxConnectionsPerServer = table.Column<int>(type: "int", nullable: true),
                    EnableMultipleHttp2Connections = table.Column<bool>(type: "bit", nullable: true),
                    RequestHeaderEncoding = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyHttpClientOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyHttpClientOptions_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProxyRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: true),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AuthorizationPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorsPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyRoutes_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequestProxyOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityTimeout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VersionPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowResponseBuffering = table.Column<bool>(type: "bit", nullable: true),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestProxyOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestProxyOptions_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionAffinityOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: true),
                    Policy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailurePolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffinityKeyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAffinityOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAffinityOptions_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActiveHealthCheckOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: true),
                    Interval = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timeout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Policy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthCheckOptionsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveHealthCheckOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActiveHealthCheckOptions_HealthCheckOptions_HealthCheckOptionsId",
                        column: x => x.HealthCheckOptionsId,
                        principalTable: "HealthCheckOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PassiveHealthCheckOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: true),
                    Policy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReactivationPeriod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthCheckOptionsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassiveHealthCheckOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassiveHealthCheckOptions_HealthCheckOptions_HealthCheckOptionsId",
                        column: x => x.HealthCheckOptionsId,
                        principalTable: "HealthCheckOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebProxyConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BypassOnLocal = table.Column<bool>(type: "bit", nullable: true),
                    UseDefaultCredentials = table.Column<bool>(type: "bit", nullable: true),
                    HttpClientConfigId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebProxyConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebProxyConfig_ProxyHttpClientOptions_HttpClientConfigId",
                        column: x => x.HttpClientConfigId,
                        principalTable: "ProxyHttpClientOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metadatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClusterId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DestinationId = table.Column<int>(type: "int", nullable: true),
                    ProxyRouteId = table.Column<int>(type: "int", nullable: true),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metadatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Metadatas_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Metadatas_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Metadatas_ProxyRoutes_ProxyRouteId",
                        column: x => x.ProxyRouteId,
                        principalTable: "ProxyRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProxyMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Methods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hosts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProxyRouteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyMatches_ProxyRoutes_ProxyRouteId",
                        column: x => x.ProxyRouteId,
                        principalTable: "ProxyRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transforms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ProxyRouteId = table.Column<int>(type: "int", nullable: true),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transforms_ProxyRoutes_ProxyRouteId",
                        column: x => x.ProxyRouteId,
                        principalTable: "ProxyRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionAffinityCookie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HttpOnly = table.Column<bool>(type: "bit", nullable: true),
                    SecurePolicy = table.Column<int>(type: "int", nullable: true),
                    SameSite = table.Column<int>(type: "int", nullable: true),
                    Expiration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxAge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEssential = table.Column<bool>(type: "bit", nullable: true),
                    SessionAffinityConfigId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAffinityCookie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAffinityCookie_SessionAffinityOptions_SessionAffinityConfigId",
                        column: x => x.SessionAffinityConfigId,
                        principalTable: "SessionAffinityOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    IsCaseSensitive = table.Column<bool>(type: "bit", nullable: false),
                    ProxyMatchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteHeaders_ProxyMatches_ProxyMatchId",
                        column: x => x.ProxyMatchId,
                        principalTable: "ProxyMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteQueryParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    IsCaseSensitive = table.Column<bool>(type: "bit", nullable: false),
                    ProxyMatchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteQueryParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteQueryParameter_ProxyMatches_ProxyMatchId",
                        column: x => x.ProxyMatchId,
                        principalTable: "ProxyMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveHealthCheckOptions_HealthCheckOptionsId",
                table: "ActiveHealthCheckOptions",
                column: "HealthCheckOptionsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_ClusterId",
                table: "Destinations",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckOptions_ClusterId",
                table: "HealthCheckOptions",
                column: "ClusterId",
                unique: true,
                filter: "[ClusterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Metadatas_ClusterId",
                table: "Metadatas",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Metadatas_DestinationId",
                table: "Metadatas",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_Metadatas_ProxyRouteId",
                table: "Metadatas",
                column: "ProxyRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_PassiveHealthCheckOptions_HealthCheckOptionsId",
                table: "PassiveHealthCheckOptions",
                column: "HealthCheckOptionsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProxyHttpClientOptions_ClusterId",
                table: "ProxyHttpClientOptions",
                column: "ClusterId",
                unique: true,
                filter: "[ClusterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyMatches_ProxyRouteId",
                table: "ProxyMatches",
                column: "ProxyRouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProxyRoutes_ClusterId",
                table: "ProxyRoutes",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestProxyOptions_ClusterId",
                table: "RequestProxyOptions",
                column: "ClusterId",
                unique: true,
                filter: "[ClusterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RouteHeaders_ProxyMatchId",
                table: "RouteHeaders",
                column: "ProxyMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteQueryParameter_ProxyMatchId",
                table: "RouteQueryParameter",
                column: "ProxyMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAffinityCookie_SessionAffinityConfigId",
                table: "SessionAffinityCookie",
                column: "SessionAffinityConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionAffinityOptions_ClusterId",
                table: "SessionAffinityOptions",
                column: "ClusterId",
                unique: true,
                filter: "[ClusterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transforms_ProxyRouteId",
                table: "Transforms",
                column: "ProxyRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_WebProxyConfig_HttpClientConfigId",
                table: "WebProxyConfig",
                column: "HttpClientConfigId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveHealthCheckOptions");

            migrationBuilder.DropTable(
                name: "Metadatas");

            migrationBuilder.DropTable(
                name: "PassiveHealthCheckOptions");

            migrationBuilder.DropTable(
                name: "RequestProxyOptions");

            migrationBuilder.DropTable(
                name: "RouteHeaders");

            migrationBuilder.DropTable(
                name: "RouteQueryParameter");

            migrationBuilder.DropTable(
                name: "SessionAffinityCookie");

            migrationBuilder.DropTable(
                name: "SessionAffinityOptionSettings");

            migrationBuilder.DropTable(
                name: "Transforms");

            migrationBuilder.DropTable(
                name: "WebProxyConfig");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "HealthCheckOptions");

            migrationBuilder.DropTable(
                name: "ProxyMatches");

            migrationBuilder.DropTable(
                name: "SessionAffinityOptions");

            migrationBuilder.DropTable(
                name: "ProxyHttpClientOptions");

            migrationBuilder.DropTable(
                name: "ProxyRoutes");

            migrationBuilder.DropTable(
                name: "Clusters");
        }
    }
}
