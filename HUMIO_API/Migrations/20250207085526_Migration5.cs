using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HUMIO_API.Migrations
{
    /// <inheritdoc />
    public partial class Migration5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryPromoCodes_DeviceIdentifiers_DeviceIdentifierId",
                table: "TemporaryPromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_TemporaryPromoCodes_DeviceIdentifierId",
                table: "TemporaryPromoCodes");

            migrationBuilder.DropColumn(
                name: "DeviceIdentifierId",
                table: "TemporaryPromoCodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeviceIdentifierId",
                table: "TemporaryPromoCodes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryPromoCodes_DeviceIdentifierId",
                table: "TemporaryPromoCodes",
                column: "DeviceIdentifierId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryPromoCodes_DeviceIdentifiers_DeviceIdentifierId",
                table: "TemporaryPromoCodes",
                column: "DeviceIdentifierId",
                principalTable: "DeviceIdentifiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
