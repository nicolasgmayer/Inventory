namespace Inventory.Models
{
    public class Productos
    {
        public int Id { get; set; } // Clave primaria
        public string Nombre { get; set; } // Nombre del producto
        public string Descripcion { get; set; } // Descripción del producto
        public decimal Precio { get; set; } // Precio del producto
        public string? ImagenPath { get; set; } // Ruta de la imagen (puede ser null)
    }
}

