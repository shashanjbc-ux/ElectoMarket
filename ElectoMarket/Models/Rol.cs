using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ElectoMarket.Models
{
    public class Rol
    {
        [Key]
        public int IdRol { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        // Relación: Un rol puede tener muchos usuarios
        public virtual ICollection<Usuario>? Usuarios { get; set; }
    }
}