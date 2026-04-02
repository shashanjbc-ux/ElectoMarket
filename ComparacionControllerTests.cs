using ElectoMarket.Controllers;
using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    // FakeSession para que el test pueda leer/escribir como si fuera un navegador
    public class FakeSessionComparacion : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public string Id => "fake-id";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        public Task CommitAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task LoadAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;

        public void SetString(string key, string value) => _store[key] = Encoding.UTF8.GetBytes(value);
        public string? GetString(string key) => _store.TryGetValue(key, out var b) ? Encoding.UTF8.GetString(b) : null;
    }

    public class ComparacionControllerTests
    {
        private (ComparacionController controller, FakeSessionComparacion session) CrearControlador(ApplicationDbContext context, List<int>? inicial = null)
        {
            var session = new FakeSessionComparacion();
            if (inicial != null) session.SetString("ListaComparacion", JsonConvert.SerializeObject(inicial));

            var mockContext = new Moq.Mock<HttpContext>();
            mockContext.Setup(c => c.Session).Returns(session);

            var controller = new ComparacionController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = mockContext.Object }
            };
            return (controller, session);
        }

        private static DbContextOptions<ApplicationDbContext> Opciones() =>
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        [Fact]
        public void Agregar_ProductoNuevo_LoGuardaEnSesion()
        {
            using var context = new ApplicationDbContext(Opciones());
            var (controller, session) = CrearControlador(context);

            controller.AgregarAComparacion(50, irAVersus: false);

            var lista = JsonConvert.DeserializeObject<List<int>>(session.GetString("ListaComparacion")!);
            Assert.Contains(50, lista);
        }

        [Fact]
        public void Agregar_ProductoDuplicado_NoLoRepite()
        {
            using var context = new ApplicationDbContext(Opciones());
            var (controller, session) = CrearControlador(context, inicial: new List<int> { 7 });

            controller.AgregarAComparacion(7);

            var lista = JsonConvert.DeserializeObject<List<int>>(session.GetString("ListaComparacion")!);
            Assert.Single(lista);
        }

        [Fact]
        public void Quitar_EliminaIdCorrecto()
        {
            using var context = new ApplicationDbContext(Opciones());
            var (controller, session) = CrearControlador(context, inicial: new List<int> { 10, 20 });

            controller.Quitar(10);

            var lista = JsonConvert.DeserializeObject<List<int>>(session.GetString("ListaComparacion")!);
            Assert.DoesNotContain(10, lista);
            Assert.Single(lista);
        }

        [Fact]
        public void LimpiarTodo_VaciaLaSesion()
        {
            using var context = new ApplicationDbContext(Opciones());
            var (controller, session) = CrearControlador(context, inicial: new List<int> { 1, 2 });

            controller.LimpiarTodo();

            Assert.Null(session.GetString("ListaComparacion"));
        }
    }
}