using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectoMarket.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.com$", ErrorMessage = "El correo debe terminar en .com")]
        public string Correo { get; set; } = string.Empty;

        //  VALIDACIÓN DE CONTRASEÑA FUERTE
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", ErrorMessage = "La contraseña debe tener mínimo 8 letras, incluir una mayúscula, un número y un carácter especial (ej: @, #, *).")]
        public string Contrasena { get; set; } = string.Empty;

        //  CONFIRMAR CONTRASEÑA (No se guarda en la base de datos)
        [NotMapped]
        [Required(ErrorMessage = "Debes confirmar tu contraseña")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        // =====================================
        //  PROPIEDADES DE PERFIL ACTUALIZADAS
        // =====================================
        public string? Descripcion { get; set; }
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Por favor ingresa tu ciudad para continuar.")]
        public string Ciudad { get; set; }

        public string? FotoPerfilUrl { get; set; }

        // =====================================
        //  RELACIÓN CON LOS ROLES
        // =====================================
        public int RolId { get; set; } = 2;

        [ForeignKey("RolId")]
        public virtual Rol? Rol { get; set; }

        public DateTime? UltimaConexion { get; set; }

        // =====================================
        //  NAVEGACIÓN: RELACIONES DEL USUARIO
        // =====================================

        // Un usuario tiene muchos productos
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

        // Un usuario tiene muchos mensajes (enviados)
        public virtual ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();

        // Un usuario participa en muchos chats
        public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();
    }
}