using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK.API.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Payments_PaymentId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Transactions");

            migrationBuilder.AddColumn<string>(
                name: "Payment",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payment",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentId",
                table: "Transactions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Payments_PaymentId",
                table: "Transactions",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
