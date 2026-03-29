using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ElectoMarket.Models; // <-- ¡Esta es la línea mágica que faltaba!

namespace ElectoMarket.Entities
{
    public class Guia
    {
        [Key]
        public int IdGuia { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;
        public int IdUsuario { get; set; }

        public virtual Usuario? Usuario { get; set; }
        public virtual ICollection<ArchivoGuia>? Archivos { get; set; }
    }
}