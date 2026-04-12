using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ElectoMarket.Models
{
    public class EditPerfilViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede tener más de 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.com$", ErrorMessage = "El correo debe ser válido y terminar obligatoriamente en .com")]
        public string Correo { get; set; } = string.Empty;

        [RegularExpression(@"^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 números y empezar por 3.")]
        public string? Telefono { get; set; }

        public string? Ciudad { get; set; }

        [StringLength(1000, ErrorMessage = "Máximo 1000 caracteres")]
        [RegularExpression(@"^\s*(?:\S+\s+){29,}\S+\s*$", ErrorMessage = "La descripción debe tener al menos 30 palabras.")]
        public string? Descripcion { get; set; }

        public string? FotoPerfilUrl { get; set; }

        public IFormFile? NuevaFoto { get; set; }
    }
}