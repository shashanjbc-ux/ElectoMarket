using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectoMarket.Migrations
{
    /// <inheritdoc />
    public partial class QuitarColumnaAudioUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🛑 ¡LO DEJAMOS VACÍO A PROPÓSITO! 🛑
            // Como la columna ya la borraste en SQL Server, 
            // le decimos a Visual Studio que no haga nada aquí.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "Mensajes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}