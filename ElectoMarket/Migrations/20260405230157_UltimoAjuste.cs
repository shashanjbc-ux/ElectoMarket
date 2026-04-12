using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectoMarket.Migrations
{
    /// <inheritdoc />
    public partial class UltimoAjuste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guias_Usuarios_IdUsuario",
                table: "Guias");

            migrationBuilder.AddForeignKey(
                name: "FK_Guias_Usuarios_IdUsuario",
                table: "Guias",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guias_Usuarios_IdUsuario",
                table: "Guias");

            migrationBuilder.AddForeignKey(
                name: "FK_Guias_Usuarios_IdUsuario",
                table: "Guias",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
