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
        [RegularExpression(@"^(?![0-9])(?!(.*\d){6}).*$", ErrorMessage = "El nombre no puede empezar con números y permite un máximo de 5 números en total.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        //  Usamos StringLength con MinimumLength: Esto cuenta TODO (letras, números, espacios)
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "La descripción debe tener al menos 20 caracteres en total.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(50000, 20000000, ErrorMessage = "El precio debe ser mínimo $50.000 y máximo $20.000.000")]
        public decimal Precio { get; set; }

        public string? Categoria { get; set; }

        public string? ImagenUrl { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, 20, ErrorMessage = "La cantidad debe estar entre 1 y 20")]
        public int Cantidad { get; set; }

        public bool RequiereReparacion { get; set; }

        [Required(ErrorMessage = "La fecha de publicación es obligatoria")]
        public DateTime FechaPublicacion { get; set; }

        [Display(Name = "Estado de Venta")]
        public bool Vendido { get; set; } = false; // Por defecto arranca en false (disponible)

        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}