using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HUMIO_API.Migrations
{
    /// <inheritdoc />
    public partial class Migration4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndDate",
                table: "UserData",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PermanentPromoCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ExtensionDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermanentPromoCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemporaryPromoCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ExtensionDays = table.Column<int>(type: "integer", nullable: false),
                    DeviceIdentifierId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryPromoCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporaryPromoCodes_DeviceIdentifiers_DeviceIdentifierId",
                        column: x => x.DeviceIdentifierId,
                        principalTable: "DeviceIdentifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PermanentPromoCodeUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PermanentPromoCodeId = table.Column<int>(type: "integer", nullable: false),
                    UsedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermanentPromoCodeUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermanentPromoCodeUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermanentPromoCodeUsages_PermanentPromoCodes_PermanentPromo~",
                        column: x => x.PermanentPromoCodeId,
                        principalTable: "PermanentPromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermanentPromoCodeUsages_PermanentPromoCodeId",
                table: "PermanentPromoCodeUsages",
                column: "PermanentPromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermanentPromoCodeUsages_UserId_PermanentPromoCodeId",
                table: "PermanentPromoCodeUsages",
                columns: new[] { "UserId", "PermanentPromoCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryPromoCodes_DeviceIdentifierId",
                table: "TemporaryPromoCodes",
                column: "DeviceIdentifierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermanentPromoCodeUsages");

            migrationBuilder.DropTable(
                name: "TemporaryPromoCodes");

            migrationBuilder.DropTable(
                name: "PermanentPromoCodes");

            migrationBuilder.DropColumn(
                name: "TrialEndDate",
                table: "UserData");
        }
    }
}
