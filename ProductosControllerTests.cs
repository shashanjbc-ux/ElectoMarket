using ElectoMarket.Controllers;
using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class FakeSessionProductos : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void SetInt32(string key, int value)
        {
            byte[] bytes = new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
            Set(key, bytes);
        }

        public void SetString(string key, string value) => Set(key, System.Text.Encoding.UTF8.GetBytes(value));
        public string? GetString(string key) => _store.TryGetValue(key, out var b) ? System.Text.Encoding.UTF8.GetString(b) : null;
    }

    public class ProductosControllerTests
    {
        // ─────────────────────────────────────────────
        // 🛠️ HELPERS: "Fábricas" de Datos Válidos
        // ─────────────────────────────────────────────
        private static DbContextOptions<ApplicationDbContext> OpcionesInMemory()
            => new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ProdDB_" + Guid.NewGuid().ToString())
                .Options;

        // 🟢 FÁBRICA DE USUARIOS: 100% Compatible con tu Usuario.cs
        private Usuario CrearUsuarioValido(string nombre)
        {
            return new Usuario
            {
                Nombre = nombre,
                Correo = $"{nombre.Replace(" ", "").ToLower()}@tienda.com",
                Contrasena = "Admin123@",         // Cumple tu Regex (Mayúscula, número, especial)
                ConfirmarContrasena = "Admin123@", // Cumple el Compare
                Telefono = "+573000000000",
                Ciudad = "Bogotá",                 // 🟢 ¡EL CAMPO QUE CAUSABA EL CRASH!
                RolId = 2
            };
        }

        // 🟢 FÁBRICA DE PRODUCTOS: 100% Compatible con tu Producto.cs
        private Producto CrearProductoValido(string nombre, decimal precio, int idVendedor)
        {
            return new Producto
            {
                Nombre = nombre,
                Precio = precio,
                Cantidad = 1,                      // 🟢 Requerido en tu modelo
                Categoria = "Tecnología",
                Descripcion = "Producto de prueba genérico",
                ImagenUrl = "/img/placeholder.png",
                FechaPublicacion = DateTime.Now,
                RequiereReparacion = false,
                IdUsuario = idVendedor
            };
        }

        private ProductosController PrepararControlador(ApplicationDbContext context, int? usuarioId = 1, int rolId = 2)
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            var tempPath = Path.Combine(Path.GetTempPath(), "ElectroMarketTests");
            Directory.CreateDirectory(Path.Combine(tempPath, "imagenes", "productos"));
            mockEnv.Setup(m => m.WebRootPath).Returns(tempPath);

            var session = new FakeSessionProductos();
            if (usuarioId.HasValue)
            {
                session.SetInt32("UsuarioId", usuarioId.Value);
                session.SetInt32("UsuarioRol", rolId);
            }

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Session).Returns(session);

            return new ProductosController(context, mockEnv.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object }
            };
        }

        // ─────────────────────────────────────────────
        // 🧪 1. CATÁLOGO Y FILTROS (Búsqueda Avanzada)
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Index_BusquedaPorNombre_FiltraCorrectamente()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());

            var vendedor = CrearUsuarioValido("TechStore");
            context.Usuarios.Add(vendedor);
            await context.SaveChangesAsync();

            context.Productos.Add(CrearProductoValido("Laptop Asus", 1000, vendedor.IdUsuario));
            context.Productos.Add(CrearProductoValido("Mouse Logitech", 50, vendedor.IdUsuario));
            await context.SaveChangesAsync();

            var controller = PrepararControlador(context);
            var resultado = await controller.Index("Laptop", null, null, null, null, null);

            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsAssignableFrom<IEnumerable<Producto>>(viewResult.Model);

            Assert.Single(modelo);
            Assert.Equal("Laptop Asus", modelo.First().Nombre);
        }

        [Fact]
        public async Task Index_FiltroPrecio_RetornaSoloProductosEnRango()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());

            var vendedor = CrearUsuarioValido("Vendedor");
            context.Usuarios.Add(vendedor);
            await context.SaveChangesAsync();

            context.Productos.Add(CrearProductoValido("Barato", 10, vendedor.IdUsuario));
            context.Productos.Add(CrearProductoValido("Medio", 50, vendedor.IdUsuario));
            context.Productos.Add(CrearProductoValido("Caro", 100, vendedor.IdUsuario));
            await context.SaveChangesAsync();

            var controller = PrepararControlador(context);
            var resultado = await controller.Index(null, null, null, precioMin: 40, precioMax: 60, null);

            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsAssignableFrom<IEnumerable<Producto>>(viewResult.Model);

            Assert.Single(modelo);
            Assert.Equal("Medio", modelo.First().Nombre);
        }

        // ─────────────────────────────────────────────
        // 🧪 2. SEGURIDAD EN EDICIÓN Y PANEL ADMIN
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Gestionar_UsuarioNormalIntentaEntrar_LoPateaAlInicio()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var controller = PrepararControlador(context, usuarioId: 5, rolId: 2);

            var resultado = await controller.Gestionar(null, null, null, null);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Delete_UsuarioNormalIntentaBorrarProductoAjeno_LoRedirige()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());

            var vendedorDueno = CrearUsuarioValido("Dueño");
            context.Usuarios.Add(vendedorDueno);
            await context.SaveChangesAsync();

            var prod = CrearProductoValido("PC Gamer", 500, vendedorDueno.IdUsuario);
            context.Productos.Add(prod);
            await context.SaveChangesAsync();

            var controller = PrepararControlador(context, usuarioId: 5, rolId: 2);
            var resultado = await controller.Delete(prod.IdProducto);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        // ─────────────────────────────────────────────
        // 🧪 3. COMPARACIÓN Y LÍMITES
        // ─────────────────────────────────────────────

        [Fact]
        public void AgregarAComparacion_LimiteDe4Productos_NoAgregaElQuinto()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());

            var vendedor = CrearUsuarioValido("Test");
            context.Usuarios.Add(vendedor);
            context.SaveChanges();

            for (int i = 1; i <= 5; i++)
            {
                context.Productos.Add(CrearProductoValido($"Prod {i}", 10, vendedor.IdUsuario));
            }
            context.SaveChanges();

            var controller = PrepararControlador(context);
            var session = (FakeSessionProductos)controller.ControllerContext.HttpContext.Session;
            session.SetString("ListaComparacion", JsonConvert.SerializeObject(new List<int> { 1, 2, 3, 4 }));

            controller.AgregarAComparacion(5);

            var json = session.GetString("ListaComparacion");
            var lista = JsonConvert.DeserializeObject<List<int>>(json!);
            Assert.Equal(4, lista!.Count);
            Assert.DoesNotContain(5, lista);
        }
    }
}