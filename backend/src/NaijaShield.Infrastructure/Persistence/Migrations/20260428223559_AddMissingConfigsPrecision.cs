using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaijaShield.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingConfigsPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interaction_Customers_CustomerId",
                table: "Interaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interaction",
                table: "Interaction");

            migrationBuilder.RenameTable(
                name: "Interaction",
                newName: "Interactions");

            migrationBuilder.RenameIndex(
                name: "IX_Interaction_CustomerId",
                table: "Interactions",
                newName: "IX_Interactions_CustomerId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Recall",
                table: "ModelDeployments",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Precision",
                table: "ModelDeployments",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "F1Score",
                table: "ModelDeployments",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RolloutPercentage",
                table: "FeatureFlags",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SentimentScore",
                table: "Conversations",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Interactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "SentimentScore",
                table: "Interactions",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "Interactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DetectedLanguage",
                table: "Interactions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                table: "Interactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interactions",
                table: "Interactions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type_ProcessedAt",
                table: "OutboxMessages",
                columns: new[] { "Type", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelDeployments_TenantId",
                table: "ModelDeployments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_TenantId",
                table: "Interactions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interactions_Customers_CustomerId",
                table: "Interactions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interactions_Customers_CustomerId",
                table: "Interactions");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Type_ProcessedAt",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_ModelDeployments_TenantId",
                table: "ModelDeployments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interactions",
                table: "Interactions");

            migrationBuilder.DropIndex(
                name: "IX_Interactions_TenantId",
                table: "Interactions");

            migrationBuilder.RenameTable(
                name: "Interactions",
                newName: "Interaction");

            migrationBuilder.RenameIndex(
                name: "IX_Interactions_CustomerId",
                table: "Interaction",
                newName: "IX_Interaction_CustomerId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Recall",
                table: "ModelDeployments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Precision",
                table: "ModelDeployments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "F1Score",
                table: "ModelDeployments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "RolloutPercentage",
                table: "FeatureFlags",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SentimentScore",
                table: "Conversations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Interaction",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "SentimentScore",
                table: "Interaction",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "Interaction",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "DetectedLanguage",
                table: "Interaction",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<int>(
                name: "Channel",
                table: "Interaction",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interaction",
                table: "Interaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Interaction_Customers_CustomerId",
                table: "Interaction",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
