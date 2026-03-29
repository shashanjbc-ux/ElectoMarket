using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// ⚠️ MUCHO OJO AQUÍ: Debe decir ElectoMarket (sin la R)
namespace ElectoMarket.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        public decimal Precio { get; set; }

        public string? Categoria { get; set; }

        public string? ImagenUrl { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        public int Cantidad { get; set; }

        public bool RequiereReparacion { get; set; }

        [Required(ErrorMessage = "La fecha de publicación es obligatoria")]
        public DateTime FechaPublicacion { get; set; }

        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}