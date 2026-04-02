using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectoMarket.Models
{
    public class Mensaje
    {
        [Key]
        public int IdMensaje { get; set; }

        public int ChatId { get; set; }
        public int RemitenteId { get; set; }

        // 🟢 Quitamos el [Required] para que se puedan enviar solo fotos o audios
        public string? Texto { get; set; }

        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        // Campo para saber si fue editado
        public bool FueEditado { get; set; } = false;

        // ==========================================
        // 🟢 NUEVOS CAMPOS PARA MULTIMEDIA
        // ==========================================
        public string? ImagenUrl { get; set; }
        public string? AudioUrl { get; set; }

        // Relaciones
        [ForeignKey("ChatId")]
        public virtual Chat? Chat { get; set; }

        [ForeignKey("RemitenteId")]
        public virtual Usuario? Remitente { get; set; }

        // 🟢 Propiedad para saber si el mensaje ya fue visto
        public bool Leido { get; set; } = false;
    }
}