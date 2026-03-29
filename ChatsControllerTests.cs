using ElectoMarket.Controllers;
using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    // ─────────────────────────────────────────────────────────────
    // 🧩 FAKE SESSION (Optimizada con Big-Endian)
    // ─────────────────────────────────────────────────────────────
    public class FakeSession : ISession
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

        // 🧙‍♂️ El truco definitivo: Guardamos los bytes en formato Big-Endian
        // exactamente como GetInt32() de ASP.NET Core los quiere leer.
        public void SetInt32(string key, int value)
        {
            byte[] bytes = new byte[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            Set(key, bytes);
        }
    }

    public class ChatsControllerTests
    {
        // ─────────────────────────────────────────────
        // 🛠️ HELPERS: Creadores de mundos falsos
        // ─────────────────────────────────────────────

        private static DbContextOptions<ApplicationDbContext> OpcionesInMemory()
            => new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ChatDB_" + Guid.NewGuid().ToString())
                .Options;

        private IFormFile CrearArchivoFalso(string nombre = "archivo_test.png")
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(nombre);
            mock.Setup(f => f.Length).Returns(1024);
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock.Object;
        }

        private ChatsController PrepararControlador(ApplicationDbContext context, int? usuarioId, int rolId = 2)
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));

            var session = new FakeSession();
            if (usuarioId.HasValue)
            {
                session.SetInt32("UsuarioId", usuarioId.Value);
                session.SetInt32("UsuarioRol", rolId);
            }

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Session).Returns(session);

            return new ChatsController(context, mockEnv.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object }
            };
        }

        // ─────────────────────────────────────────────
        // 🧪 PRUEBAS: 1. VISTA PRINCIPAL E INICIAR CHAT
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Index_SinLogin_RedirigeALogin()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var controller = PrepararControlador(context, usuarioId: null); // Sin login

            var resultado = await controller.Index(null);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Usuarios", redirect.ControllerName);
        }

        [Fact]
        public async Task IniciarChat_NuevoChat_LoCreaYRedirige()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var controller = PrepararControlador(context, usuarioId: 1); // Yo soy el 1

            var resultado = await controller.IniciarChat(receptorId: 2); // Quiero chatear con el 2

            var chatCreado = await context.Chats.FirstOrDefaultAsync();
            Assert.NotNull(chatCreado);
            Assert.Equal(1, chatCreado.Usuario1Id);
            Assert.Equal(2, chatCreado.Usuario2Id);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
        }

        // ─────────────────────────────────────────────
        // 🧪 PRUEBAS: 2. ENVIAR MENSAJES (Multimedia)
        // ─────────────────────────────────────────────

        [Fact]
        public async Task EnviarMensaje_Vacio_NoGuardaNada()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var controller = PrepararControlador(context, usuarioId: 1);

            await controller.EnviarMensaje(10, null, null, null);

            Assert.Empty(context.Mensajes); // Validamos que no se guardó basura
        }

        [Fact]
        public async Task EnviarMensaje_ConTextoYFoto_GuardaAmbos()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var controller = PrepararControlador(context, usuarioId: 1);
            var foto = CrearArchivoFalso("laptop.jpg");

            await controller.EnviarMensaje(chatId: 5, texto: "Mira esta laptop", fotoAdjunta: foto, audioAdjunto: null);

            var msg = await context.Mensajes.FirstOrDefaultAsync();
            Assert.NotNull(msg);
            Assert.Equal("Mira esta laptop", msg.Texto);
            Assert.Contains(".jpg", msg.ImagenUrl);
        }

        // ─────────────────────────────────────────────
        // 🧪 PRUEBAS: 3. SEGURIDAD DE ADMIN (EDITAR/ELIMINAR)
        // ─────────────────────────────────────────────

        [Fact]
        public async Task EditarMensaje_SiendoAdmin_EditaYAgregaFirma()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var msg = new Mensaje { IdMensaje = 10, ChatId = 1, RemitenteId = 2, Texto = "Original" };
            context.Mensajes.Add(msg);
            await context.SaveChangesAsync();

            var controller = PrepararControlador(context, usuarioId: 1, rolId: 1); // ROL 1 = ADMIN

            await controller.EditarMensaje(10, "Editado", 1);

            var msgActualizado = await context.Mensajes.FindAsync(10);
            Assert.Contains("(Modificado por Admin)", msgActualizado!.Texto);
            Assert.True(msgActualizado.FueEditado);
        }

        [Fact]
        public async Task EliminarMensaje_SiendoUsuarioNormal_RetornaForbid()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());
            var msg = new Mensaje { IdMensaje = 20, ChatId = 1, RemitenteId = 2, Texto = "Privado" };
            context.Mensajes.Add(msg);
            await context.SaveChangesAsync();

            var controller = PrepararControlador(context, usuarioId: 2, rolId: 2); // ROL 2 = NORMAL

            var resultado = await controller.EliminarMensaje(20, 1);

            Assert.IsType<ForbidResult>(resultado); // Se le prohíbe el paso
            Assert.Single(context.Mensajes); // El mensaje no se borró
        }

        // ─────────────────────────────────────────────
        // 🧪 PRUEBAS: 4. PERSONALIZACIÓN DE CHAT
        // ─────────────────────────────────────────────

        [Fact]
        public async Task PersonalizarChat_CambiarColorYFondo_ActualizaUsuarioCorrecto()
        {
            using var context = new ApplicationDbContext(OpcionesInMemory());

            // Arrange: Le ponemos el color inicial al Usuario 2 para asegurarnos
            var chat = new Chat
            {
                IdChat = 100,
                Usuario1Id = 5,
                Usuario2Id = 8,
                ColorBurbujaUsuario2 = "bg-morado text-white" // <-- El color por defecto
            };
            context.Chats.Add(chat);
            await context.SaveChangesAsync();

            var controller = PrepararControlador(context, usuarioId: 5); // Soy el Usuario 1
            var fondo = CrearArchivoFalso("mifondo.png");

            // Act: Usuario 1 personaliza su lado
            await controller.PersonalizarChat(100, colorBurbuja: "bg-danger", imagenFondo: fondo);

            // Assert
            var chatDb = await context.Chats.FindAsync(100);

            // Verificamos que al Usuario 1 SÍ se le aplicaron los cambios
            Assert.Equal("bg-danger", chatDb!.ColorBurbujaUsuario1);
            Assert.Contains(".png", chatDb.FondoPantallaUrlUsuario1);

            // 🟢 SOLUCIÓN: Verificamos que al Usuario 2 NO se le cambió el color, 
            // sigue teniendo su morado original en vez de esperar un 'null'
            Assert.Equal("bg-morado text-white", chatDb.ColorBurbujaUsuario2);
        }
    }
}