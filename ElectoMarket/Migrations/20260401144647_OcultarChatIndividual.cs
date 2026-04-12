using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectoMarket.Migrations
{
    /// <inheritdoc />
    public partial class OcultarChatIndividual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OcultoParaUsuario1",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OcultoParaUsuario2",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcultoParaUsuario1",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "OcultoParaUsuario2",
                table: "Chats");
        }
    }
}
