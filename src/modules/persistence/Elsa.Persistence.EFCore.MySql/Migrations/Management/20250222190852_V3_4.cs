#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.MySql.Migrations.Management
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
            migrationBuilder.AddColumn<bool>(
                name: "IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "IsExecuting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances");
        }
    }
}
