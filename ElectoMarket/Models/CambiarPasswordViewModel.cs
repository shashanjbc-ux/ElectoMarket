using System.ComponentModel.DataAnnotations;

namespace ElectoMarket.Models
{
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "Escribe tu contraseña actual")]
        [DataType(DataType.Password)]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).+$",
            ErrorMessage = "Debe tener: 1 Mayúscula, 1 Número y 1 Símbolo (#%$@)")]
        public string PasswordNueva { get; set; }

        [Required(ErrorMessage = "Repite la nueva contraseña")]
        [DataType(DataType.Password)]
        [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPasswordNueva { get; set; }
    }
}