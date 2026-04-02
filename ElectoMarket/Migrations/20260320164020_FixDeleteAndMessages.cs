using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectoMarket.Migrations
{
    /// <inheritdoc />
    public partial class FixDeleteAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mensajes_Chats_ChatId",
                table: "Mensajes");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioIdUsuario",
                table: "Mensajes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioIdUsuario",
                table: "Chats",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_UsuarioIdUsuario",
                table: "Mensajes",
                column: "UsuarioIdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_UsuarioIdUsuario",
                table: "Chats",
                column: "UsuarioIdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Usuarios_UsuarioIdUsuario",
                table: "Chats",
                column: "UsuarioIdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensajes_Chats_ChatId",
                table: "Mensajes",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "IdChat",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mensajes_Usuarios_UsuarioIdUsuario",
                table: "Mensajes",
                column: "UsuarioIdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Usuarios_UsuarioIdUsuario",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensajes_Chats_ChatId",
                table: "Mensajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensajes_Usuarios_UsuarioIdUsuario",
                table: "Mensajes");

            migrationBuilder.DropIndex(
                name: "IX_Mensajes_UsuarioIdUsuario",
                table: "Mensajes");

            migrationBuilder.DropIndex(
                name: "IX_Chats_UsuarioIdUsuario",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "UsuarioIdUsuario",
                table: "Mensajes");

            migrationBuilder.DropColumn(
                name: "UsuarioIdUsuario",
                table: "Chats");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensajes_Chats_ChatId",
                table: "Mensajes",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "IdChat",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
