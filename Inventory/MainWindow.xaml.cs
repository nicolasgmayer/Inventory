using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Inventory.Data;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.IO;



namespace Inventory
{
    public partial class MainWindow : Window
    {
        private const string LogFilePath = "logs/log.txt";

        public MainWindow()
        {
            InitializeComponent();
            CargarProductos();

            // Configurar un temporizador para sincronización automática cada 30 segundos
            var timer = new System.Timers.Timer(30000);  // 30 segundos
            timer.Elapsed += (s, e) => Dispatcher.Invoke(() => CargarProductos());  // Recargar productos en el DataGrid en el hilo principal
            timer.Start();
        }



        // Evento para guardar un nuevo producto
        private void SeleccionarImagen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos PNG (*.png)|*.png", // Solo acepta archivos .png
                Title = "Seleccionar una imagen"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtImagenPath.Text = openFileDialog.FileName; // Muestra la ruta del archivo seleccionado
            }
        }
        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            string nombreProducto = txtNombreProducto.Text;
            string descripcion = txtDescripcion.Text;
            decimal precio;

            if (string.IsNullOrWhiteSpace(nombreProducto))
            {
                EscribirLog("Error: El nombre del producto es obligatorio.");
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out precio) || precio <= 0)
            {
                EscribirLog("Error: El precio debe ser un número positivo.");
                return;
            }

            // Verificar que la ruta de la imagen no esté vacía
            string imagenPath = txtImagenPath.Text;
            if (string.IsNullOrWhiteSpace(imagenPath))
            {
                imagenPath = "N/A"; // Asignar un valor predeterminado si la imagen no se selecciona
            }

            // Guardar en la base de datos usando Entity Framework
            try
            {
                using (var context = new AppDbContext())
                {
                    var producto = new Productos
                    {
                        Nombre = nombreProducto,
                        Descripcion = descripcion,
                        Precio = precio,
                        ImagenPath = imagenPath // Asignar la ruta de la imagen
                    };
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                    context.Productos.Add(producto);
                    context.SaveChanges(); // Intentar guardar en la base de datos
                }

                EscribirLog($"Producto '{nombreProducto}' guardado correctamente.");
                LimpiarCampos(); // Limpiar los campos después de guardar
                CargarProductos(); // Recargar la lista de productos
            }
            catch (Exception ex)
            {
                // Obtener detalles de la excepción
                EscribirLog($"Error al guardar el producto: {ex.Message}");

                // Si hay una InnerException, también la registramos
                if (ex.InnerException != null)
                {
                    EscribirLog($"Detalles del error: {ex.InnerException.Message}");
                }
            }
        }


        // Método para cargar los productos en el DataGrid
        public void CargarProductos()
        {
            using (var context = new AppDbContext())

            {
                var productos = context.Productos.AsNoTracking().ToList();
                ProductosDataGrid.ItemsSource = productos;

            }
        }

        // Evento para generar el reporte
        // Evento para generar el reporte
        private void GenerarReporteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EscribirLog("Generación de reporte solicitada.");

                // Obtener los productos del DataGrid (suponiendo que están en el ItemsSource)
                var productos = (ProductosDataGrid.ItemsSource as System.Collections.IEnumerable).Cast<Productos>().ToList();

                if (productos.Count == 0)
                {
                    EscribirLog("No hay productos para generar el reporte.");
                    MessageBox.Show("No hay productos para generar el reporte.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Crear el libro de trabajo de Excel
                var wb = new XLWorkbook();

                // Agregar una hoja con el nombre "Productos"
                var ws = wb.Worksheets.Add("Productos");

                // Crear los encabezados de las columnas
                ws.Cell(1, 1).Value = "Id";
                ws.Cell(1, 2).Value = "Nombre";
                ws.Cell(1, 3).Value = "Descripción";
                ws.Cell(1, 4).Value = "Precio";
                ws.Cell(1, 5).Value = "Imagen Path";

                // Agregar los datos de los productos
                int row = 2; // Comenzar desde la segunda fila (para los datos)
                foreach (var producto in productos)
                {
                    ws.Cell(row, 1).Value = producto.Id;
                    ws.Cell(row, 2).Value = producto.Nombre;
                    ws.Cell(row, 3).Value = producto.Descripcion;
                    ws.Cell(row, 4).Value = producto.Precio;
                    ws.Cell(row, 5).Value = producto.ImagenPath ?? "N/A"; // Si no hay imagen, poner "N/A"
                    row++;
                }

                // Configurar el nombre del archivo y la ruta para guardarlo
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Archivo Excel (*.xlsx)|*.xlsx",
                    FileName = "Reporte_Productos_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Guardar el archivo Excel en la ruta seleccionada
                    wb.SaveAs(saveFileDialog.FileName);
                    EscribirLog($"Reporte generado y guardado en: {saveFileDialog.FileName}");
                    //MessageBox.Show("Reporte generado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                EscribirLog($"Error al generar el reporte: {ex.Message}");
                MessageBox.Show($"Error al generar el reporte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Evento para guardar cambios realizados en el DataGrid
        private void GuardarCambiosButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    // Convierte el ItemsSource del DataGrid a una lista de productos
                    var productosModificados = (ProductosDataGrid.ItemsSource as System.Collections.IEnumerable).Cast<Productos>().ToList();

                    // Recorre cada producto modificado y lo actualiza en la base de datos
                    foreach (var producto in productosModificados)
                    {
                        context.Productos.Update(producto); // Marca el producto como modificado
                    }

                    context.SaveChanges(); // Guarda los cambios en la base de datos
                }

                EscribirLog("Cambios guardados correctamente.");
            }
            catch (Exception ex)
            {
                EscribirLog($"Error al guardar los cambios: {ex.Message}");
            }
        }

        // Evento para eliminar un producto
        private void EliminarProductoButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductosDataGrid.SelectedItem is Productos productoSeleccionado)
            {
                using (var context = new AppDbContext())
                {
                    context.Productos.Remove(productoSeleccionado);
                    context.SaveChanges();

                    EscribirLog($"Producto '{productoSeleccionado.Nombre}' eliminado.");
                    CargarProductos(); // Recargar los productos después de la eliminación
                }
            }
            else
            {
                EscribirLog("Error: Selecciona un producto para eliminar.");
            }
        }

        // Método para escribir en el archivo de log
        private void EscribirLog(string mensaje)
        {
            try
            {
                // Preguntamos por directorio
                string directory = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Log
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))

                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mensaje}");






                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al escribir en el log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para limpiar los campos de texto
        private void LimpiarCampos()
        {
            txtNombreProducto.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtPrecio.Text = string.Empty;


        }

        // Evento para importar productos desde un archivo Excel
        private void ImportarExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                Title = "Seleccionar archivo Excel"
            };


            if (openFileDialog.ShowDialog() == true)




            {
                var archivo = openFileDialog.FileName;



                // Cargar productos desde el archivo Excel
                var productos = new List<Productos>();
                using (var workbook = new XLWorkbook(archivo))





                {
                    var worksheet = workbook.Worksheets.Worksheet(1); // Primer hoja
                    var rows = worksheet.RowsUsed();

                    foreach (var row in rows.Skip(1)) // Ignorar la primera fila (encabezados)
                    {
                        var producto = new Productos
                        {
                            Nombre = row.Cell(1).GetValue<string>(),
                            Descripcion = row.Cell(2).GetValue<string>(),
                            Precio = row.Cell(3).GetValue<decimal>(),
                            ImagenPath = row.Cell(4).GetValue<string>()
                        };
                        productos.Add(producto);
                    }
                }

                // Mostrar los productos en el DataGrid
                ProductosDataGrid.ItemsSource = productos;

            }
        }



    }
}
