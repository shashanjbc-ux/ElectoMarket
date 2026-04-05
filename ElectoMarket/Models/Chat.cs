using ElectoMarket.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectoMarket.Models
{
    public class Chat
    {
        [Key]
        public int IdChat { get; set; }

        public int Usuario1Id { get; set; }
        public int Usuario2Id { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // 🎨 NUEVAS PROPIEDADES SEPARADAS POR USUARIO
        public string ColorBurbujaUsuario1 { get; set; } = "bg-morado text-white";
        public string? FondoPantallaUrlUsuario1 { get; set; }

        public string ColorBurbujaUsuario2 { get; set; } = "bg-morado text-white";
        public string? FondoPantallaUrlUsuario2 { get; set; }

        // 🟢 PROPIEDADES PARA OCULTAR EL CHAT (Eliminación asimétrica)
        public bool OcultoParaUsuario1 { get; set; } = false;
        public bool OcultoParaUsuario2 { get; set; } = false;

        // Relaciones
        [ForeignKey("Usuario1Id")]
        public virtual Usuario? Usuario1 { get; set; }

        [ForeignKey("Usuario2Id")]
        public virtual Usuario? Usuario2 { get; set; }

        public virtual ICollection<Mensaje>? Mensajes { get; set; }
    }
}