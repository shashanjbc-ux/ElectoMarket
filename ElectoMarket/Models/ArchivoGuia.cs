using System;
using System.ComponentModel.DataAnnotations;

namespace ElectoMarket.Entities
{
    public class ArchivoGuia
    {
        [Key]
        public int IdArchivo { get; set; }
        public int IdGuia { get; set; }
        public string? NombreArchivo { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public string? TipoArchivo { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;

        public virtual Guia? Guia { get; set; }
    }
}