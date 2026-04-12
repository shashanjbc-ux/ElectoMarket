using System.Collections.Generic;

namespace ElectoMarket.Models
{
    public class ComparacionViewModel
    {
        // Lista de productos que el usuario eligió comparar
        public List<Producto> Productos { get; set; } = new List<Producto>();

        
    }
}