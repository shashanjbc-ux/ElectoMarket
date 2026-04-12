using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectoMarket.Models
{
    public class Guia
    {
        [Key]
        public int IdGuia { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        //  Rango: 50 caracteres (10 pal) a 150 caracteres (30 pal)
        [StringLength(150, MinimumLength = 50, ErrorMessage = "El título debe tener entre 10 y 30 palabras.")]
        [RegularExpression(@"^[^0-9]*$", ErrorMessage = "El título NO puede contener números.")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        //  Rango: 50 caracteres (10 pal) a 500 caracteres (100 pal)
        [StringLength(500, MinimumLength = 50, ErrorMessage = "La descripción debe tener entre 10 y 100 palabras.")]
        public string Descripcion { get; set; }

        // Aquí guardaremos la ruta del video (ej: /videos/guias/tutorial.mp4) o el enlace de YouTube
        public string? VideoUrl { get; set; }

        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        //  CONTADOR DE IMPACTO: Vistas del video
        [Display(Name = "Vistas")]
        public int Vistas { get; set; } = 0; // Se inicializa en 0 por defecto

        // Relación con el creador del video
        [ForeignKey("Usuario")]
        public int IdUsuario { get; set; }
        public virtual Usuario? Usuario { get; set; }
    }
}