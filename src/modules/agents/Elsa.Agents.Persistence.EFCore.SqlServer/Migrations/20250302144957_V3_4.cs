using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Agents.Persistence.EFCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class V3_4 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_4(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentConfig = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeysDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeysDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicesDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinition_Name",
                schema: _schema.Schema,
                table: "AgentDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinition_TenantId",
                schema: _schema.Schema,
                table: "AgentDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyDefinition_Name",
                schema: _schema.Schema,
                table: "ApiKeysDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyDefinition_TenantId",
                schema: _schema.Schema,
                table: "ApiKeysDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinition_Name",
                schema: _schema.Schema,
                table: "ServicesDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDefinition_TenantId",
                schema: _schema.Schema,
                table: "ServicesDefinitions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDefinitions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ApiKeysDefinitions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "ServicesDefinitions",
                schema: _schema.Schema);
        }
    }
}
