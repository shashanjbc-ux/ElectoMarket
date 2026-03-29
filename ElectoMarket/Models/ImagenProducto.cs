using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ElectoMarket.Models;

namespace ElectoMarket.Models
{
    public class ImagenProducto
    {
        [Key]
        public int IdImagen { get; set; }

        [Required]
        public string RutaImagen { get; set; } = string.Empty;

        // Relación con Producto
        public int IdProducto { get; set; }

        [ForeignKey("IdProducto")]
        public virtual Producto? Producto { get; set; }
    }
}