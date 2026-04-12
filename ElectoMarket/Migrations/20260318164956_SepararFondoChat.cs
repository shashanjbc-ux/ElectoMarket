using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectoMarket.Migrations
{
    /// <inheritdoc />
    public partial class SepararFondoChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FondoPantallaUrl",
                table: "Chats",
                newName: "FondoPantallaUrlUsuario2");

            migrationBuilder.RenameColumn(
                name: "ColorBurbuja",
                table: "Chats",
                newName: "ColorBurbujaUsuario2");

            migrationBuilder.AddColumn<string>(
                name: "ColorBurbujaUsuario1",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FondoPantallaUrlUsuario1",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorBurbujaUsuario1",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "FondoPantallaUrlUsuario1",
                table: "Chats");

            migrationBuilder.RenameColumn(
                name: "FondoPantallaUrlUsuario2",
                table: "Chats",
                newName: "FondoPantallaUrl");

            migrationBuilder.RenameColumn(
                name: "ColorBurbujaUsuario2",
                table: "Chats",
                newName: "ColorBurbuja");
        }
    }
}
