using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JuliusFinances.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    flow_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "created_at", "flow_type", "name", "owner_id", "updated_at" },
                values: new object[,]
                {
                    { new Guid("de250001-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Alimentação", null, null },
                    { new Guid("de250002-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Habitação", null, null },
                    { new Guid("de250003-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Transporte", null, null },
                    { new Guid("de250004-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Saúde", null, null },
                    { new Guid("de250005-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Educação", null, null },
                    { new Guid("de250006-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Lazer & Entretenimento", null, null },
                    { new Guid("de250007-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Serviços & Assinaturas", null, null },
                    { new Guid("de250008-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Expense", "Outras Despesas", null, null },
                    { new Guid("de250009-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Income", "Salário", null, null },
                    { new Guid("de250010-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Income", "Investimentos", null, null },
                    { new Guid("de250011-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Income", "Freelance / Serviços", null, null },
                    { new Guid("de250012-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Income", "Presentes / Prêmios", null, null },
                    { new Guid("de250013-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Income", "Outras Receitas", null, null },
                    { new Guid("de250014-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Both", "Transferência", null, null },
                    { new Guid("de250015-c812-4c22-9014-99859f123456"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Both", "Ajuste de Saldo", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
