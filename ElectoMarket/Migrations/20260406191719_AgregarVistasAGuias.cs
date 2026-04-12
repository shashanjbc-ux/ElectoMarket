using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectoMarket.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVistasAGuias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Vistas",
                table: "Guias",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Vistas",
                table: "Guias");
        }
    }
}
