using ElectoMarket.Models; // 👈 ¡Esta es la línea clave!
using System.Collections.Generic;

namespace ElectoMarket.Models // (O el namespace que estés usando para tus ViewModels)
{
    public class PerfilViewModel
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = "¡Hola! Soy un entusiasta de la tecnología en ElectroMarket.";
        public string FotoPerfil { get; set; } = "/img/default-user.png";

        // La lista de productos que este usuario ha publicado
        public List<Producto> MisProductos { get; set; } = new List<Producto>();
    }
}