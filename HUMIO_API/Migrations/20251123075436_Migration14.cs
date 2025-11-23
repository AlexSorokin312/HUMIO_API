using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HUMIO_API.Migrations
{
    /// <inheritdoc />
    public partial class Migration14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_DeviceIdentifiers_DeviceId",
                table: "DeviceIdentifiers",
                column: "DeviceId");

            migrationBuilder.CreateTable(
                name: "ApplePaymentInfos",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    PaymentCount = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revenue = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplePaymentInfos", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_ApplePaymentInfos_DeviceIdentifiers_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DeviceIdentifiers",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplePaymentInfos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DeviceIdentifiers_DeviceId",
                table: "DeviceIdentifiers");
        }
    }
}
