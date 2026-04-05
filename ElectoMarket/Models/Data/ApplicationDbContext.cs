using Microsoft.EntityFrameworkCore;
using ElectoMarket.Models;

namespace ElectoMarket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================
        // TABLAS PRINCIPALES
        // =====================================
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Rol> Roles { get; set; }

        // 🎬 TABLA DE GUÍAS (¡La nueva sección de videos!)
        public DbSet<Guia> Guias { get; set; }

        // =====================================
        // TABLAS DEL CHAT
        // =====================================
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }

        // =====================================
        // CONFIGURACIÓN AVANZADA
        // =====================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🟢 Roles por defecto
            modelBuilder.Entity<Rol>().HasData(
                new Rol { IdRol = 1, Nombre = "Admin" },
                new Rol { IdRol = 2, Nombre = "Cliente" }
            );

            // ========================================================
            // REGLAS PARA EVITAR EL ERROR DE CASCADA
            // ========================================================

            // 1. Relación Chat -> Usuario1
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Usuario1)
                .WithMany()
                .HasForeignKey(c => c.Usuario1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Relación Chat -> Usuario2
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Usuario2)
                .WithMany()
                .HasForeignKey(c => c.Usuario2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Relación Mensaje -> Remitente
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Remitente)
                .WithMany()
                .HasForeignKey(m => m.RemitenteId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Relación Mensaje -> Chat
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Mensajes)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Relación Guia -> Usuario (Para evitar errores si borras un usuario con videos)
            modelBuilder.Entity<Guia>()
                .HasOne(g => g.Usuario)
                .WithMany()
                .HasForeignKey(g => g.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}