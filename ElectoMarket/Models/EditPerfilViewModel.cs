using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ElectoMarket.Models
{
    public class EditPerfilViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Correo { get; set; } = string.Empty;

        public string? Telefono { get; set; }
        public string? Ciudad { get; set; }

        [StringLength(250, ErrorMessage = "Máximo 250 caracteres")]
        public string? Descripcion { get; set; }

        public string? FotoPerfilUrl { get; set; }
        public IFormFile? NuevaFoto { get; set; }
    }
}