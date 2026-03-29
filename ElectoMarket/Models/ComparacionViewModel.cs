using System.Collections.Generic;

namespace ElectoMarket.Models
{
    public class ComparacionViewModel
    {
        // Lista de productos que el usuario eligió comparar
        public List<Producto> Productos { get; set; } = new List<Producto>();

        // Puedes añadir aquí propiedades extra si quieres comparar atributos específicos 
        // que no estén en el modelo Producto principal en el futuro.
    }
}