using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedLocalSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDistributedLocalStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "net_discovery_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProductSlug = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    InstanceSlug = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    InstanceGuid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RemoteHostIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UdpPort = table.Column<int>(type: "INTEGER", nullable: false),
                    LanPort = table.Column<int>(type: "INTEGER", nullable: false),
                    BeaconIntervalMs = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscoveryTimeoutMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ProtocolVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_net_discovery_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "net_discovery_settings");
        }
    }
}
