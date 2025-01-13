using Microsoft.EntityFrameworkCore;
using Inventory.Models;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Inventory.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        // Constructor que inyecta la configuración
        public AppDbContext()
        {
                
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        private string GetConnectionString()
        {
            
            string user = _configuration["DBSetting:User"];
            string password = _configuration["DBSetting:Pass"];

            
            string connectionStringTemplate = _configuration.GetConnectionString("DefaultConnection");

            
            return $"{connectionStringTemplate};User ID={user};Password={password}";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
               
                var connectionString = GetConnectionString();
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        // DbSet para el modelo Producto
        public DbSet<Productos> Productos { get; set; }

        // Configura las entidades y sus columnas
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<Productos>()
                .Property(p => p.Precio)
                .HasColumnType("decimal(18,2)");  

            base.OnModelCreating(modelBuilder); 
        }
    }
}
